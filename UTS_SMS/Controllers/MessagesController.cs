using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using SMS.Services;

namespace SMS.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly MessageService _messageService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public MessagesController(
            MessageService messageService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _messageService = messageService;
            _userManager = userManager;
            _context = context;
            _environment = environment;
        }

        /// <summary>
        /// Get all messages for the current user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMessages([FromQuery] string timeFilter = "all")
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var messages = await _messageService.GetMessagesForUser(currentUser.Id, timeFilter);

                var result = messages.Select(mr => new
                {
                    id = mr.MessageId,
                    subject = mr.Message.Subject,
                    body = mr.Message.Body.Length > 100 ? mr.Message.Body.Substring(0, 100) + "..." : mr.Message.Body,
                    senderName = mr.Message.Sender?.FullName ?? "Unknown",
                    sentDate = mr.Message.SentDate,
                    isRead = mr.IsRead,
                    attachmentCount = mr.Message.Attachments?.Count ?? 0
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get unread message count
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var count = await _messageService.GetUnreadCountForUser(currentUser.Id);

                return Ok(new { count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Mark a message as read
        /// </summary>
        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var success = await _messageService.MarkAsRead(id, currentUser.Id);
                return Ok(new { success });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get message details with attachments
        /// </summary>
        [HttpGet("{id}/details")]
        public async Task<IActionResult> GetMessageDetails(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var message = await _messageService.GetMessageDetails(id, currentUser.Id);
                if (message == null)
                {
                    return NotFound(new { error = "Message not found or access denied" });
                }

                var result = new
                {
                    id = message.Id,
                    subject = message.Subject,
                    body = message.Body,
                    senderName = message.Sender?.FullName ?? "Unknown",
                    sentDate = message.SentDate,
                    attachments = message.Attachments?.Select(a => new
                    {
                        id = a.Id,
                        fileName = a.FileName,
                        filePath = a.FilePath,
                        fileSize = a.FileSize,
                        fileType = a.FileType
                    })
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Send a new message
        /// </summary>
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var subject = Request.Form["subject"].ToString();
                var body = Request.Form["body"].ToString();
                var recipientIds = Request.Form["recipientIds"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

                if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body) || !recipientIds.Any())
                {
                    return BadRequest(new { error = "Subject, body, and recipients are required" });
                }

                // Handle file uploads
                var attachments = new List<MessageAttachment>();
                var files = Request.Form.Files;

                // Define allowed file types and max size (10MB)
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt" };
                const long maxFileSize = 10 * 1024 * 1024; // 10MB

                if (files.Count > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "messages");
                    Directory.CreateDirectory(uploadsFolder);

                    foreach (var file in files)
                    {
                        if (file.Length > 0)
                        {
                            // Validate file size
                            if (file.Length > maxFileSize)
                            {
                                return BadRequest(new { error = $"File {file.FileName} exceeds the maximum size of 10MB" });
                            }

                            // Validate file extension
                            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                            if (!allowedExtensions.Contains(fileExtension))
                            {
                                return BadRequest(new { error = $"File type {fileExtension} is not allowed. Allowed types: {string.Join(", ", allowedExtensions)}" });
                            }

                            // Sanitize filename to prevent path traversal attacks
                            var safeFileName = Path.GetFileName(file.FileName);
                            var uniqueFileName = $"{Guid.NewGuid()}_{safeFileName}";
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            attachments.Add(new MessageAttachment
                            {
                                FileName = safeFileName,
                                FilePath = $"/uploads/messages/{uniqueFileName}",
                                FileType = file.ContentType,
                                FileSize = file.Length
                            });
                        }
                    }
                }

                var messageId = await _messageService.SendMessage(
                    currentUser.Id,
                    subject,
                    body,
                    currentUser.CampusId ?? 0,
                    recipientIds,
                    attachments
                );

                return Ok(new { success = true, messageId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get students by class and section
        /// </summary>
        [HttpGet("students")]
        public async Task<IActionResult> GetStudents([FromQuery] int classId, [FromQuery] int sectionId)
        {
            try
            {
                var students = await _messageService.GetStudentsByClassSection(classId, sectionId);

                var result = students.Select(s => new
                {
                    id = s.Id,
                    name = s.FullName,
                    email = s.Email
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get employees by role
        /// </summary>
        [HttpGet("employees")]
        public async Task<IActionResult> GetEmployees([FromQuery] string role, [FromQuery] int? campusId = null)
        {
            try
            {
                var employees = await _context.Employees
                    .Where(e => e.Role == role)
                    .Where(e => !campusId.HasValue || e.CampusId == campusId.Value)
                    .ToListAsync();

                var employeeIds = employees.Select(e => e.Id).ToList();

                var users = await _context.Users
                    .Where(u => u.EmployeeId.HasValue && employeeIds.Contains(u.EmployeeId.Value))
                    .ToListAsync();

                var result = employees.Select(e => {
                    var user = users.FirstOrDefault(u => u.EmployeeId == e.Id);
                    return new
                    {
                        id = user?.Id,
                        name = e.FullName,
                        email = user?.Email
                    };
                }).Where(e => e.id != null).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get admins by campus
        /// </summary>
        [HttpGet("admins")]
        public async Task<IActionResult> GetAdmins([FromQuery] int campusId)
        {
            try
            {
                var admins = await _messageService.GetAdminsByCampus(campusId);

                var result = admins.Select(a => new
                {
                    id = a.Id,
                    name = a.FullName,
                    email = a.Email
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
