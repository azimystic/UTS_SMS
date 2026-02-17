using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using UTS_SMS.Models;
using UTS_SMS.Services;

namespace UTS_SMS.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time AI chat streaming.
    /// Handles message sending, conversation management, and PDF ingestion progress.
    /// </summary>
    [Authorize(Roles = "Admin,Teacher,Student")]
    public class AiChatHub : Hub
    {
        private readonly AiChatService _chatService;
        private readonly PdfIngestionService _ingestionService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AiChatHub> _logger;

        public AiChatHub(
            AiChatService chatService,
            PdfIngestionService ingestionService,
            UserManager<ApplicationUser> userManager,
            ILogger<AiChatHub> logger)
        {
            _chatService = chatService;
            _ingestionService = ingestionService;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Send a message to the AI and stream the response back.
        /// </summary>
        public async Task SendMessage(int? conversationId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                await Clients.Caller.SendAsync("Error", "Message cannot be empty.");
                return;
            }

            try
            {
                var userContext = await GetUserContextAsync();
                if (userContext == null)
                {
                    await Clients.Caller.SendAsync("Error", "Authentication failed.");
                    return;
                }

                _logger.LogInformation("AI Chat: User {UserId} ({Role}) sent message", userContext.UserId, userContext.Role);

                var reader = _chatService.StreamChatAsync(message, conversationId, userContext, Context.ConnectionAborted);

                while (await reader.WaitToReadAsync(Context.ConnectionAborted))
                {
                    while (reader.TryRead(out var evt))
                    {
                        switch (evt.Type)
                        {
                            case ChatEventType.ConversationCreated:
                                await Clients.Caller.SendAsync("ConversationCreated", int.Parse(evt.Data));
                                break;

                            case ChatEventType.ThinkingStep:
                                await Clients.Caller.SendAsync("ThinkingStep", evt.Data);
                                break;

                            case ChatEventType.StreamStarted:
                                await Clients.Caller.SendAsync("StreamStarted");
                                break;

                            case ChatEventType.ContentChunk:
                                await Clients.Caller.SendAsync("ContentChunk", evt.Data);
                                break;

                            case ChatEventType.SourceCitation:
                                await Clients.Caller.SendAsync("SourceCitation", evt.Data);
                                break;

                            case ChatEventType.StreamComplete:
                                await Clients.Caller.SendAsync("StreamComplete", int.Parse(evt.Data));
                                break;

                            case ChatEventType.Error:
                                await Clients.Caller.SendAsync("Error", evt.Data);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AI chat hub");
                await Clients.Caller.SendAsync("Error", "An unexpected error occurred. Please try again.");
            }
        }

        /// <summary>
        /// Get the user's conversation list.
        /// </summary>
        public async Task GetConversations()
        {
            var user = await _userManager.GetUserAsync(Context.User!);
            if (user == null) return;

            var conversations = await _chatService.GetConversationsAsync(user.Id);
            await Clients.Caller.SendAsync("ConversationsList", conversations);
        }

        /// <summary>
        /// Load messages for a specific conversation.
        /// </summary>
        public async Task LoadConversation(int conversationId)
        {
            var user = await _userManager.GetUserAsync(Context.User!);
            if (user == null) return;

            var messages = await _chatService.GetConversationMessagesAsync(conversationId, user.Id);
            await Clients.Caller.SendAsync("ConversationMessages", conversationId, messages);
        }

        /// <summary>
        /// Delete a conversation.
        /// </summary>
        public async Task DeleteConversation(int conversationId)
        {
            var user = await _userManager.GetUserAsync(Context.User!);
            if (user == null) return;

            var success = await _chatService.DeleteConversationAsync(conversationId, user.Id);
            await Clients.Caller.SendAsync("ConversationDeleted", conversationId, success);
        }

        /// <summary>
        /// Trigger PDF ingestion (Admin only).
        /// </summary>
        public async Task IngestDocuments()
        {
            var user = await _userManager.GetUserAsync(Context.User!);
            if (user == null) return;

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Admin"))
            {
                await Clients.Caller.SendAsync("Error", "Only admins can trigger document ingestion.");
                return;
            }

            try
            {
                var (processed, failed, skipped) = await _ingestionService.IngestAllPdfsAsync(
                    progress => Clients.Caller.SendAsync("IngestionProgress", progress).Wait());

                await Clients.Caller.SendAsync("IngestionComplete",
                    $"Ingestion complete: {processed} processed, {failed} failed, {skipped} skipped.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during document ingestion");
                await Clients.Caller.SendAsync("Error", $"Ingestion failed: {ex.Message}");
            }
        }

        // ── Private Helpers ─────────────────────────────────────────────

        private async Task<UserContext?> GetUserContextAsync()
        {
            var user = await _userManager.GetUserAsync(Context.User!);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.Contains("Admin") ? "Admin"
                            : roles.Contains("Teacher") ? "Teacher"
                            : roles.Contains("Student") ? "Student"
                            : roles.FirstOrDefault() ?? "Student";

            return new UserContext
            {
                UserId = user.Id,
                FullName = user.FullName ?? user.Email ?? "User",
                Role = primaryRole,
                StudentId = user.StudentId,
                EmployeeId = user.EmployeeId,
                CampusId = user.CampusId
            };
        }
    }
}
