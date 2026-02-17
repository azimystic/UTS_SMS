using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using UTS_SMS.Models;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;

namespace UTS_SMS.Services
{
    /// <summary>
    /// Orchestrates AI chat using Semantic Kernel with Groq (OpenAI-compatible) + auto function calling.
    /// </summary>
    public class AiChatService
    {
        private readonly ApplicationDbContext _context;
        private readonly AiChatOptions _options;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AiChatService> _logger;

        public AiChatService(
            ApplicationDbContext context,
            IOptions<AiChatOptions> options,
            IServiceProvider serviceProvider,
            ILogger<AiChatService> logger)
        {
            _context = context;
            _options = options.Value;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Stream an AI response with automatic tool invocation.
        /// Returns a ChannelReader of ChatStreamEvent for the SignalR hub to consume.
        /// </summary>
        public ChannelReader<ChatStreamEvent> StreamChatAsync(
            string userMessage,
            int? conversationId,
            UserContext userContext,
            CancellationToken cancellationToken = default)
        {
            var channel = Channel.CreateUnbounded<ChatStreamEvent>();

            _ = Task.Run(async () =>
            {
                try
                {
                    await ProcessChatAsync(channel.Writer, userMessage, conversationId, userContext, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during AI chat streaming");
                    await channel.Writer.WriteAsync(new ChatStreamEvent
                    {
                        Type = ChatEventType.Error,
                        Data = $"An error occurred while generating the response. Please try again.\n\nDebug: {ex.Message}"
                    }, cancellationToken);
                }
                finally
                {
                    channel.Writer.Complete();
                }
            }, cancellationToken);

            return channel.Reader;
        }

        private async Task ProcessChatAsync(
            ChannelWriter<ChatStreamEvent> writer,
            string userMessage,
            int? conversationId,
            UserContext userContext,
            CancellationToken cancellationToken)
        {
            Exception? lastException = null;
            foreach (var model in _options.GroqModels)
            {
                try
                {
                    var kernelBuilder = Kernel.CreateBuilder();
                    kernelBuilder.AddOpenAIChatCompletion(
                        modelId: model,
                        apiKey: _options.GroqApiKey,
                        endpoint: new Uri(_options.GroqApiUrl));

                    var kernel = kernelBuilder.Build();

                    // Create and configure the plugin
                    var plugin = _serviceProvider.GetRequiredService<SmsPlugin>();
                    plugin.CurrentStudentId = userContext.StudentId;
                    plugin.CurrentCampusId = userContext.CampusId;
                    plugin.CurrentRole = userContext.Role;
                    plugin.CurrentEmployeeId = userContext.EmployeeId;

                    kernel.ImportPluginFromObject(plugin, "SchoolTools");

                    // ── 2. Load or create conversation ──────────────────────────
                    AiChatConversation conversation;
                    ChatHistory chatHistory = new();

                    if (conversationId.HasValue)
                    {
                        conversation = await _context.AiChatConversations
                            .Include(c => c.Messages)
                            .FirstOrDefaultAsync(c => c.Id == conversationId.Value && c.UserId == userContext.UserId, cancellationToken)
                            ?? CreateNewConversation(userContext);

                        // Rebuild chat history from saved messages
                        foreach (var msg in conversation.Messages.OrderBy(m => m.Timestamp))
                        {
                            switch (msg.Role)
                            {
                                case ChatRole.User:
                                    chatHistory.AddUserMessage(msg.Content);
                                    break;
                                case ChatRole.Assistant:
                                    chatHistory.AddAssistantMessage(msg.Content);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        conversation = CreateNewConversation(userContext);
                        _context.AiChatConversations.Add(conversation);
                        await _context.SaveChangesAsync(cancellationToken);
                    }

                    // ── 3. System prompt (role-specific) ────────────────────────
                    var systemPrompt = BuildSystemPrompt(userContext);
                    chatHistory.AddSystemMessage(systemPrompt);
                    chatHistory.AddUserMessage(userMessage);

                    // Save user message
                    conversation.Messages.Add(new AiChatMessage
                    {
                        ConversationId = conversation.Id,
                        Role = Models.ChatRole.User,
                        Content = userMessage,
                        Timestamp = DateTime.Now
                    });
                    conversation.LastMessageAt = DateTime.Now;

                    // Auto-generate title from first message
                    if (conversation.Title == "New Chat")
                    {
                        conversation.Title = userMessage.Length > 50
                            ? userMessage.Substring(0, 50) + "..."
                            : userMessage;
                    }

                    await _context.SaveChangesAsync(cancellationToken);

                    // Emit conversation ID
                    await writer.WriteAsync(new ChatStreamEvent
                    {
                        Type = ChatEventType.ConversationCreated,
                        Data = conversation.Id.ToString()
                    }, cancellationToken);

                    // ── 4. Execute with auto function calling ───────────────────
                    var executionSettings = new OpenAIPromptExecutionSettings
                    {
                        Temperature = 0.7,
                        MaxTokens = 2048
                    };

                    var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
                    var previousToolCallCount = 0;
                    var fullResponse = new System.Text.StringBuilder();
                    var streamingStarted = false;

                    await foreach (var chunk in chatCompletionService.GetStreamingChatMessageContentsAsync(
                        chatHistory, executionSettings, kernel, cancellationToken))
                    {
                        // Check for new tool calls
                        if (plugin.ToolCallLog.Count > previousToolCallCount)
                        {
                            for (int i = previousToolCallCount; i < plugin.ToolCallLog.Count; i++)
                            {
                                await writer.WriteAsync(new ChatStreamEvent
                                {
                                    Type = ChatEventType.ThinkingStep,
                                    Data = plugin.ToolCallLog[i].Description
                                }, cancellationToken);
                            }
                            previousToolCallCount = plugin.ToolCallLog.Count;
                        }

                        if (!string.IsNullOrEmpty(chunk.Content))
                        {
                            if (!streamingStarted)
                            {
                                await writer.WriteAsync(new ChatStreamEvent
                                {
                                    Type = ChatEventType.StreamStarted,
                                    Data = ""
                                }, cancellationToken);
                                streamingStarted = true;
                            }

                            fullResponse.Append(chunk.Content);
                            await writer.WriteAsync(new ChatStreamEvent
                            {
                                Type = ChatEventType.ContentChunk,
                                Data = chunk.Content
                            }, cancellationToken);
                        }
                    }

                    // Emit any remaining tool call steps
                    if (plugin.ToolCallLog.Count > previousToolCallCount)
                    {
                        for (int i = previousToolCallCount; i < plugin.ToolCallLog.Count; i++)
                        {
                            await writer.WriteAsync(new ChatStreamEvent
                            {
                                Type = ChatEventType.ThinkingStep,
                                Data = plugin.ToolCallLog[i].Description
                            }, cancellationToken);
                        }
                    }

                    // Emit source citations
                    if (plugin.SourcesCited.Any())
                    {
                        var uniqueSources = plugin.SourcesCited
                            .GroupBy(s => s.FileName + s.PageNumber)
                            .Select(g => g.First())
                            .ToList();

                        foreach (var source in uniqueSources)
                        {
                            await writer.WriteAsync(new ChatStreamEvent
                            {
                                Type = ChatEventType.SourceCitation,
                                Data = JsonSerializer.Serialize(new
                                {
                                    fileName = source.FileName,
                                    filePath = source.FilePath,
                                    pageNumber = source.PageNumber,
                                    chapterName = source.ChapterName,
                                    subjectName = source.SubjectName
                                })
                            }, cancellationToken);
                        }
                    }

                    // Save assistant response
                    var sourcesJson = plugin.SourcesCited.Any()
                        ? JsonSerializer.Serialize(plugin.SourcesCited.Select(s => new
                        {
                            s.FileName,
                            s.FilePath,
                            s.PageNumber,
                            s.ChapterName,
                            s.SubjectName
                        }))
                        : null;

                    conversation.Messages.Add(new AiChatMessage
                    {
                        ConversationId = conversation.Id,
                        Role = Models.ChatRole.Assistant,
                        Content = fullResponse.ToString(),
                        Sources = sourcesJson,
                        Timestamp = DateTime.Now
                    });

                    await _context.SaveChangesAsync(cancellationToken);

                    await writer.WriteAsync(new ChatStreamEvent
                    {
                        Type = ChatEventType.StreamComplete,
                        Data = conversation.Id.ToString()
                    }, cancellationToken);

                    // If we got here, the model worked, so break out of the fallback loop
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    // Try next model
                }
            }
            // If all models fail, emit error
            await writer.WriteAsync(new ChatStreamEvent
            {
                Type = ChatEventType.Error,
                Data = $"All Groq models failed. Last error: {lastException?.Message}"
            }, cancellationToken);
        }

        /// <summary>
        /// Get all conversations for a user.
        /// </summary>
        public async Task<List<ConversationSummary>> GetConversationsAsync(string userId)
        {
            return await _context.AiChatConversations
                .Where(c => c.UserId == userId && c.IsActive)
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .Select(c => new ConversationSummary
                {
                    Id = c.Id,
                    Title = c.Title,
                    CreatedAt = c.CreatedAt,
                    LastMessageAt = c.LastMessageAt,
                    MessageCount = c.Messages.Count
                })
                .ToListAsync();
        }

        /// <summary>
        /// Get messages for a specific conversation.
        /// </summary>
        public async Task<List<MessageDto>> GetConversationMessagesAsync(int conversationId, string userId)
        {
            return await _context.AiChatMessages
                .Where(m => m.ConversationId == conversationId
                            && m.Conversation!.UserId == userId
                            && m.Role != Models.ChatRole.Tool)
                .OrderBy(m => m.Timestamp)
                .Select(m => new MessageDto
                {
                    Id = m.Id,
                    Role = m.Role.ToString().ToLower(),
                    Content = m.Content,
                    Sources = m.Sources,
                    Timestamp = m.Timestamp
                })
                .ToListAsync();
        }

        /// <summary>
        /// Delete a conversation.
        /// </summary>
        public async Task<bool> DeleteConversationAsync(int conversationId, string userId)
        {
            var conversation = await _context.AiChatConversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

            if (conversation == null) return false;

            conversation.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        // ── Private Helpers ─────────────────────────────────────────────────

        private AiChatConversation CreateNewConversation(UserContext ctx)
        {
            return new AiChatConversation
            {
                UserId = ctx.UserId,
                Title = "New Chat",
                CampusId = ctx.CampusId ?? 0,
                CreatedAt = DateTime.Now
            };
        }

        private string BuildSystemPrompt(UserContext ctx)
        {
            var basePrompt = @"You are a helpful, friendly School AI Assistant for the School Management System. 
You help users understand academic performance, study materials, and school information.
Always be encouraging and supportive. When discussing grades, provide constructive feedback.
When citing information from study materials, mention the source document and page number.
Use clear formatting with bullet points and headers when presenting data.
If you don't have enough information to answer accurately, say so rather than guessing.
Today's date is " + DateTime.Now.ToString("MMMM dd, yyyy") + ".\n\n";

            switch (ctx.Role)
            {
                case "Student":
                    return basePrompt +
                           $"The current user is a STUDENT named '{ctx.FullName}' (Student ID: {ctx.StudentId}). " +
                           $"They can only ask about their OWN grades, attendance, and study materials. " +
                           $"When they ask about 'my grades' or 'how did I do', use their Student ID ({ctx.StudentId}) to look up data. " +
                           $"Be supportive and provide study suggestions based on their performance.";

                case "Teacher":
                    return basePrompt +
                           $"The current user is a TEACHER named '{ctx.FullName}' (Employee ID: {ctx.EmployeeId}). " +
                           $"They can look up grades and performance data for students in their assigned classes. " +
                           $"They can also search study materials and view class-wide performance statistics. " +
                           $"Provide analytical insights and suggestions for improving class performance.";

                case "Admin":
                    return basePrompt +
                           $"The current user is an ADMIN named '{ctx.FullName}'. " +
                           $"They have full access to all student data, grades, attendance, and study materials within their campus. " +
                           $"Provide comprehensive data analysis and actionable insights.";

                default:
                    return basePrompt + "You are helping a school staff member.";
            }
        }
    }

    // ── DTOs ────────────────────────────────────────────────────────────────

    public class UserContext
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Student";
        public int? StudentId { get; set; }
        public int? EmployeeId { get; set; }
        public int? CampusId { get; set; }
    }

    public enum ChatEventType
    {
        ConversationCreated,
        ThinkingStep,
        StreamStarted,
        ContentChunk,
        SourceCitation,
        StreamComplete,
        Error
    }

    public class ChatStreamEvent
    {
        public ChatEventType Type { get; set; }
        public string Data { get; set; } = string.Empty;
    }

    public class ConversationSummary
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public int MessageCount { get; set; }
    }

    public class MessageDto
    {
        public int Id { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Sources { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
