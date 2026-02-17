using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UTS_SMS.Models;
using UTS_SMS.Services;

namespace UTS_SMS.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SimpleChatController : ControllerBase
    {
        private readonly SimpleChatService _chatService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SimpleChatController> _logger;

        public SimpleChatController(
            SimpleChatService chatService,
            UserManager<ApplicationUser> userManager,
            ILogger<SimpleChatController> logger)
        {
            _chatService = chatService;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { success = false, message = "Message cannot be empty." });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { success = false, message = "User not authenticated." });
            }

            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.Contains("Admin") ? "Admin"
                            : roles.Contains("Teacher") ? "Teacher"
                            : roles.Contains("Student") ? "Student"
                            : roles.FirstOrDefault() ?? "User";

            var userContext = new ChatUserContext
            {
                UserId = user.Id,
                FullName = user.FullName ?? user.Email ?? "User",
                Role = primaryRole,
                StudentId = user.StudentId,
                EmployeeId = user.EmployeeId,
                CampusId = user.CampusId
            };

            // Convert history
            var conversationHistory = request.History?.Select(h => new ChatMessage
            {
                Role = h.Role,
                Content = h.Content
            }).ToList() ?? new List<ChatMessage>();

            var response = await _chatService.ProcessMessageAsync(
                request.Message,
                conversationHistory,
                userContext);

            return Ok(new
            {
                success = response.Success,
                message = response.Message
            });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<ChatHistoryItem>? History { get; set; }
    }

    public class ChatHistoryItem
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
    }
}
