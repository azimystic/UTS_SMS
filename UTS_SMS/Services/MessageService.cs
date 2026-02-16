using UTS_SMS.Models;
using Microsoft.EntityFrameworkCore;

namespace SMS.Services
{
    public class MessageService
    {
        private readonly ApplicationDbContext _context;

        public MessageService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get messages for a specific user with time filtering
        /// </summary>
        public async Task<List<MessageRecipient>> GetMessagesForUser(string userId, string timeFilter = "all")
        {
            var query = _context.MessageRecipients
                .Include(mr => mr.Message)
                    .ThenInclude(m => m.Sender)
                .Include(mr => mr.Message)
                    .ThenInclude(m => m.Attachments)
                .Where(mr => mr.UserId == userId);

            // Apply time filtering
            if (timeFilter == "today")
            {
                var today = DateTime.Today;
                query = query.Where(mr => mr.Message.SentDate >= today);
            }
            else if (timeFilter == "yesterday")
            {
                var yesterday = DateTime.Today.AddDays(-1);
                var today = DateTime.Today;
                query = query.Where(mr => mr.Message.SentDate >= yesterday && mr.Message.SentDate < today);
            }

            return await query.OrderByDescending(mr => mr.Message.SentDate).ToListAsync();
        }

        /// <summary>
        /// Get unread message count for a user
        /// </summary>
        public async Task<int> GetUnreadCountForUser(string userId)
        {
            return await _context.MessageRecipients
                .Where(mr => mr.UserId == userId && !mr.IsRead)
                .CountAsync();
        }

        /// <summary>
        /// Mark a message as read for a specific recipient
        /// </summary>
        public async Task<bool> MarkAsRead(int messageId, string userId)
        {
            var recipient = await _context.MessageRecipients
                .FirstOrDefaultAsync(mr => mr.MessageId == messageId && mr.UserId == userId);

            if (recipient == null || recipient.IsRead)
                return false;

            recipient.IsRead = true;
            recipient.ReadAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Get message details with attachments
        /// </summary>
        public async Task<Message?> GetMessageDetails(int messageId, string userId)
        {
            // Verify the user is a recipient of this message
            var isRecipient = await _context.MessageRecipients
                .AnyAsync(mr => mr.MessageId == messageId && mr.UserId == userId);

            if (!isRecipient)
                return null;

            return await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Attachments)
                .Include(m => m.Recipients)
                .FirstOrDefaultAsync(m => m.Id == messageId);
        }

        /// <summary>
        /// Send a message to multiple recipients
        /// </summary>
        public async Task<int> SendMessage(string senderId, string subject, string body, int campusId, List<string> recipientUserIds, List<MessageAttachment>? attachments = null)
        {
            var message = new Message
            {
                SenderId = senderId,
                Subject = subject,
                Body = body,
                SentDate = DateTime.Now,
                CampusId = campusId
            };

            // Add recipients
            foreach (var recipientId in recipientUserIds.Distinct())
            {
                message.Recipients.Add(new MessageRecipient
                {
                    UserId = recipientId,
                    IsRead = false
                });
            }

            // Add attachments if any
            if (attachments != null && attachments.Any())
            {
                foreach (var attachment in attachments)
                {
                    message.Attachments.Add(attachment);
                }
            }

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return message.Id;
        }

        /// <summary>
        /// Get students by class and section for recipient selection
        /// </summary>
        public async Task<List<ApplicationUser>> GetStudentsByClassSection(int classId, int sectionId)
        {
            var studentIds = await _context.Students
                .Where(s => s.Class == classId && s.Section == sectionId)
                .Select(s => s.Id)
                .ToListAsync();

            return await _context.Users
                .Where(u => u.StudentId.HasValue && studentIds.Contains(u.StudentId.Value))
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        /// <summary>
        /// Get employees by role for recipient selection
        /// </summary>
        public async Task<List<ApplicationUser>> GetEmployeesByRole(string role, int? campusId = null)
        {
            var employeeIds = await _context.Employees
                .Where(e => e.Role == role)
                .Where(e => !campusId.HasValue || e.CampusId == campusId.Value)
                .Select(e => e.Id)
                .ToListAsync();

            return await _context.Users
                .Where(u => u.EmployeeId.HasValue && employeeIds.Contains(u.EmployeeId.Value))
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        /// <summary>
        /// Get all admins by campus for recipient selection
        /// </summary>
        public async Task<List<ApplicationUser>> GetAdminsByCampus(int campusId)
        {
            var adminEmployeeIds = await _context.Employees
                .Where(e => e.Role == "Admin" && e.CampusId == campusId)
                .Select(e => e.Id)
                .ToListAsync();

            return await _context.Users
                .Where(u => u.EmployeeId.HasValue && adminEmployeeIds.Contains(u.EmployeeId.Value))
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }
    }
}
