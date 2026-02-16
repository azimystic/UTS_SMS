using UTS_SMS.Models;
using Microsoft.EntityFrameworkCore;

namespace SMS.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Create a notification for student complaint
        /// </summary>
        public async Task CreateComplaintNotification(StudentComplaint complaint)
        {
            var notification = new Notification
            {
                Type = "complaint",
                Title = "New Student Complaint",
                Message = $"A new complaint has been registered by Student #{complaint.StudentId} regarding {complaint.ComplaintType}.",
                Timestamp = DateTime.Now,
                TargetRole = "Admin",
                RelatedEntityId = complaint.Id,
                RelatedEntityType = "StudentComplaint",
                ActionUrl = $"/StudentComplaint/Details/{complaint.Id}",
                CampusId = complaint.CampusId,
                CreatedBy = complaint.CreatedBy
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Create a notification for fee payment reminder
        /// </summary>
        public async Task CreateFeeReminderNotification(int studentId, decimal outstandingAmount, int campusId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return;

            var notification = new Notification
            {
                Type = "fee",
                Title = "Fee Payment Reminder",
                Message = $"Student #{studentId} ({student.StudentName}) has an outstanding fee payment of {outstandingAmount:C} due.",
                Timestamp = DateTime.Now,
                TargetRole = "Admin,Accountant",
                RelatedEntityId = studentId,
                RelatedEntityType = "Student",
                ActionUrl = $"/Billing/Index?studentId={studentId}",
                CampusId = campusId,
                CreatedBy = "System"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Create a notification for upcoming exam
        /// </summary>
        public async Task CreateExamNotification(ExamDateSheet examDateSheet)
        {
            var notification = new Notification
            {
                Type = "exam",
                Title = "Upcoming Exam",
                Message = $"{examDateSheet.Subject?.Name ?? "Exam"} exam scheduled for {examDateSheet.ExamDate:dd/MM/yyyy}.",
                Timestamp = DateTime.Now,
                TargetRole = "Admin,Teacher",
                RelatedEntityId = examDateSheet.Id,
                RelatedEntityType = "ExamDateSheet",
                ActionUrl = $"/ExamDateSheet/Details/{examDateSheet.Id}",
                CampusId = examDateSheet.CampusId,
                CreatedBy = "System"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Create a general notification
        /// </summary>
        public async Task CreateGeneralNotification(string title, string message, string targetRole, int campusId, string? actionUrl = null)
        {
            var notification = new Notification
            {
                Type = "general",
                Title = title,
                Message = message,
                Timestamp = DateTime.Now,
                TargetRole = targetRole,
                ActionUrl = actionUrl,
                CampusId = campusId,
                CreatedBy = "System"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Get notifications for a specific user based on their role and campus
        /// </summary>
        public async Task<List<Notification>> GetNotificationsForUser(string? userId, string userRole, int campusId)
        {
            var query = _context.Notifications
                .Where(n => n.IsActive && n.CampusId == campusId);

            // Filter by user ID if specified, or by role
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(n => n.UserId == userId || n.UserId == null);
            }

            // Filter by role
            if (!string.IsNullOrEmpty(userRole))
            {
                query = query.Where(n => n.TargetRole == null || 
                                        n.TargetRole.Contains(userRole));
            }

            var notifications = await query
                .OrderByDescending(n => n.Timestamp)
                .Take(50)
                .ToListAsync();

            // For each notification, check if current user has read it
            if (!string.IsNullOrEmpty(userId))
            {
                var readNotificationIds = await _context.UserNotificationReads
                    .Where(unr => unr.UserId == userId)
                    .Select(unr => unr.NotificationId)
                    .ToListAsync();

                // Mark notifications as read/unread based on user-specific tracking
                foreach (var notification in notifications)
                {
                    notification.IsRead = readNotificationIds.Contains(notification.Id);
                }
            }

            return notifications;
        }

        /// <summary>
        /// Mark a notification as read for a specific user
        /// </summary>
        public async Task MarkAsRead(int notificationId, string userId)
        {
            // Check if user already marked this as read
            var existingRead = await _context.UserNotificationReads
                .FirstOrDefaultAsync(unr => unr.NotificationId == notificationId && unr.UserId == userId);

            if (existingRead == null)
            {
                // Create new read record
                var userNotificationRead = new UserNotificationRead
                {
                    NotificationId = notificationId,
                    UserId = userId,
                    ReadAt = DateTime.Now
                };

                _context.UserNotificationReads.Add(userNotificationRead);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Mark all notifications as read for a user
        /// </summary>
        public async Task MarkAllAsRead(string? userId, string userRole, int campusId)
        {
            if (string.IsNullOrEmpty(userId)) return;

            var notifications = await GetNotificationsForUser(userId, userRole, campusId);
            
            var unreadNotifications = notifications.Where(n => !n.IsRead).ToList();
            
            if (!unreadNotifications.Any()) return;

            // Get already read notification IDs to avoid duplicates
            var notificationIds = unreadNotifications.Select(n => n.Id).ToList();
            var alreadyReadIds = await _context.UserNotificationReads
                .Where(unr => unr.UserId == userId && notificationIds.Contains(unr.NotificationId))
                .Select(unr => unr.NotificationId)
                .ToListAsync();

            // Create read records for notifications not already marked as read
            var newReadRecords = unreadNotifications
                .Where(n => !alreadyReadIds.Contains(n.Id))
                .Select(n => new UserNotificationRead
                {
                    NotificationId = n.Id,
                    UserId = userId,
                    ReadAt = DateTime.Now
                })
                .ToList();

            if (newReadRecords.Any())
            {
                // Use a transaction to ensure atomicity
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.UserNotificationReads.AddRange(newReadRecords);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        /// <summary>
        /// Get unread notification count for a user
        /// </summary>
        public async Task<int> GetUnreadCount(string? userId, string userRole, int campusId)
        {
            if (string.IsNullOrEmpty(userId)) return 0;

            var query = _context.Notifications
                .Where(n => n.IsActive && n.CampusId == campusId);

            // Filter by user ID if specified, or by role
            query = query.Where(n => n.UserId == userId || n.UserId == null);

            // Filter by role
            if (!string.IsNullOrEmpty(userRole))
            {
                query = query.Where(n => n.TargetRole == null || 
                                        n.TargetRole.Contains(userRole));
            }

            var allNotificationIds = await query.Select(n => n.Id).ToListAsync();

            // Get notifications this user has already read
            var readNotificationIds = await _context.UserNotificationReads
                .Where(unr => unr.UserId == userId && allNotificationIds.Contains(unr.NotificationId))
                .Select(unr => unr.NotificationId)
                .ToListAsync();

            // Unread count = all notifications - read notifications
            return allNotificationIds.Count - readNotificationIds.Count;
        }

        /// <summary>
        /// Create a notification when fee is received
        /// </summary>
        public async Task CreateFeeReceivedNotification(int studentId, decimal amountPaid, int campusId, string createdBy)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return;

            var notification = new Notification
            {
                Type = "fee",
                Title = "Fee Payment Received",
                Message = $"Fee payment of {amountPaid:C} received from {student.StudentName} (ID: {studentId}).",
                Timestamp = DateTime.Now,
                TargetRole = "Admin,Accountant",
                RelatedEntityId = studentId,
                RelatedEntityType = "BillingTransaction",
                ActionUrl = $"/Billing/Index?studentId={studentId}",
                CampusId = campusId,
                CreatedBy = createdBy
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Create notification for student when marks are entered
        /// </summary>
        public async Task CreateMarksEntryNotification(int studentId, int examId, int subjectId, decimal obtainedMarks, decimal totalMarks, int campusId, string createdBy)
        {
            var student = await _context.Students.FindAsync(studentId);
            var exam = await _context.Exams.FindAsync(examId);
            var subject = await _context.Subjects.FindAsync(subjectId);
            
            if (student == null || exam == null || subject == null) return;

            // Get the user associated with the student via ApplicationUser table
            // Since there's no direct User navigation, we'll send to all students in the notification system
            // The frontend filtering will handle showing it only to the correct student
            var notification = new Notification
            {
                Type = "marks",
                Title = "Exam Marks Uploaded",
                Message = $"Marks for {subject.Name} - {exam.Name} have been uploaded: {obtainedMarks}/{totalMarks}. Check your dashboard for details.",
                Timestamp = DateTime.Now,
                TargetRole = "Student",
                UserId = null, // Will be filtered by student when they login
                RelatedEntityId = studentId,
                RelatedEntityType = "ExamMarks",
                ActionUrl = $"/StudentDashboard/Index",
                CampusId = campusId,
                CreatedBy = createdBy
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Create reminder notification for teacher who hasn't entered marks
        /// </summary>
        public async Task CreateMarksEntryReminderNotification(int teacherId, int examId, DateTime examDate, int campusId)
        {
            var teacher = await _context.Employees.FindAsync(teacherId);
            var exam = await _context.Exams.FindAsync(examId);
            
            if (teacher == null || exam == null) return;

            var daysSinceExam = (DateTime.Now - examDate).Days;

            // Get the teacher's user account
            var teacherUser = await _context.Users
                .FirstOrDefaultAsync(u => u.EmployeeId == teacherId);

            // Notification for specific teacher only
            var notification = new Notification
            {
                Type = "reminder",
                Title = "Marks Entry Reminder",
                Message = $"Reminder: You haven't entered marks for {exam.Name} which was held {daysSinceExam} days ago on {examDate:dd/MM/yyyy}.",
                Timestamp = DateTime.Now,
                TargetRole = "Teacher",
                UserId = teacherUser?.Id, // Send to specific teacher
                RelatedEntityId = examId,
                RelatedEntityType = "Exam",
                ActionUrl = $"/ExamMarks/Entry",
                CampusId = campusId,
                CreatedBy = "System"
            };

            _context.Notifications.Add(notification);

            // Also notify admin users in this campus
            var adminUserIds = await GetAdminUserIdsForCampus(campusId);

            foreach (var adminUserId in adminUserIds)
            {
                var adminNotification = new Notification
                {
                    Type = "reminder",
                    Title = "Teacher Marks Entry Pending",
                    Message = $"{teacher.FullName} hasn't entered marks for {exam.Name} (held {daysSinceExam} days ago).",
                    Timestamp = DateTime.Now,
                    TargetRole = "Admin",
                    UserId = adminUserId, // Send to specific admin
                    RelatedEntityId = examId,
                    RelatedEntityType = "Exam",
                    ActionUrl = $"/ExamMarks/Entry",
                    CampusId = campusId,
                    CreatedBy = "System"
                };
                _context.Notifications.Add(adminNotification);
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Create reminder notification for teacher who hasn't added diary
        /// </summary>
        public async Task CreateDiaryReminderNotification(int teacherId, int teacherAssignmentId, DateTime lectureDate, int campusId)
        {
            var teacher = await _context.Employees.FindAsync(teacherId);
            var assignment = await _context.TeacherAssignments
                .Include(ta => ta.Subject)
                .Include(ta => ta.Class)
                .Include(ta => ta.Section)
                .FirstOrDefaultAsync(ta => ta.Id == teacherAssignmentId);
            
            if (teacher == null || assignment == null) return;

            // Get the teacher's user account
            var teacherUser = await _context.Users
                .FirstOrDefaultAsync(u => u.EmployeeId == teacherId);

            // Notification for specific teacher only
            var notification = new Notification
            {
                Type = "reminder",
                Title = "Diary Entry Reminder",
                Message = $"Reminder: You haven't added diary for {assignment.Subject?.Name} - {assignment.Class?.Name} {assignment.Section?.Name} on {lectureDate:dd/MM/yyyy}.",
                Timestamp = DateTime.Now,
                TargetRole = "Teacher",
                UserId = teacherUser?.Id, // Send to specific teacher
                RelatedEntityId = teacherAssignmentId,
                RelatedEntityType = "TeacherAssignment",
                ActionUrl = $"/Diary/Create",
                CampusId = campusId,
                CreatedBy = "System"
            };

            _context.Notifications.Add(notification);

            // Also notify admin users in this campus
            var adminUserIds = await GetAdminUserIdsForCampus(campusId);

            foreach (var adminUserId in adminUserIds)
            {
                var adminNotification = new Notification
                {
                    Type = "reminder",
                    Title = "Teacher Diary Entry Pending",
                    Message = $"{teacher.FullName} hasn't added diary for {assignment.Subject?.Name} - {assignment.Class?.Name} {assignment.Section?.Name} on {lectureDate:dd/MM/yyyy}.",
                    Timestamp = DateTime.Now,
                    TargetRole = "Admin",
                    UserId = adminUserId, // Send to specific admin
                    RelatedEntityId = teacherAssignmentId,
                    RelatedEntityType = "TeacherAssignment",
                    ActionUrl = $"/Diary/Index",
                    CampusId = campusId,
                    CreatedBy = "System"
                };
                _context.Notifications.Add(adminNotification);
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Check and send reminders for teachers who haven't entered marks after 3 days
        /// </summary>
        public async Task CheckAndSendMarksEntryReminders()
        {
            var threeDaysAgo = DateTime.Now.AddDays(-3).Date;

            // Find exam date sheets from exams that happened 3+ days ago
            var pendingExams = await _context.ExamDateSheets
                .Include(eds => eds.Exam)
                .Include(eds => eds.Subject)
                .Include(eds => eds.ClassSections)
                    .ThenInclude(cs => cs.Class)
                .Include(eds => eds.ClassSections)
                    .ThenInclude(cs => cs.Section)
                .Where(eds => eds.ExamDate <= threeDaysAgo && eds.IsActive)
                .ToListAsync();

            foreach (var examDateSheet in pendingExams)
            {
                // Process each class-section combination
                foreach (var classSection in examDateSheet.ClassSections.Where(cs => cs.IsActive))
                {
                    // Get students in this class/section
                    var students = await _context.Students
                        .Where(s => s.Class == classSection.ClassId && 
                                   s.Section == classSection.SectionId && 
                                   !s.HasLeft)
                        .Select(s => s.Id)
                        .ToListAsync();

                    if (!students.Any()) continue;

                    // Check if marks have been entered for all students
                    var marksEntered = await _context.ExamMarks
                        .Where(em => em.ExamId == examDateSheet.ExamId &&
                                   em.SubjectId == examDateSheet.SubjectId &&
                                   em.ClassId == classSection.ClassId &&
                                   em.SectionId == classSection.SectionId &&
                                   students.Contains(em.StudentId) &&
                                   em.IsActive)
                        .CountAsync();

                    // If marks not entered for all students, find the teacher and send reminder
                    if (marksEntered < students.Count)
                    {
                        var teacherAssignment = await _context.TeacherAssignments
                            .FirstOrDefaultAsync(ta => ta.ClassId == classSection.ClassId &&
                                                      ta.SectionId == classSection.SectionId &&
                                                      ta.SubjectId == examDateSheet.SubjectId &&
                                                      ta.IsActive);

                        if (teacherAssignment != null)
                        {
                            // Check if we already sent a reminder recently (within last 24 hours)
                            var recentReminder = await _context.Notifications
                                .Where(n => n.Type == "reminder" &&
                                          n.RelatedEntityId == examDateSheet.ExamId &&
                                          n.RelatedEntityType == "Exam" &&
                                          n.Timestamp > DateTime.Now.AddHours(-24))
                                .AnyAsync();

                            if (!recentReminder)
                            {
                                await CreateMarksEntryReminderNotification(
                                    teacherAssignment.TeacherId,
                                    examDateSheet.ExamId,
                                    examDateSheet.ExamDate,
                                    examDateSheet.CampusId
                                );
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check and send reminders for diary entries based on timetable
        /// </summary>
        public async Task CheckAndSendDiaryReminders()
        {
            var today = DateTime.Now.Date;
            var currentTime = DateTime.Now;
            var currentDayOfWeek = (int)DateTime.Now.DayOfWeek;
            if (currentDayOfWeek == 0) currentDayOfWeek = 7; // Convert Sunday from 0 to 7

            // Get all active timetables for today
            var timetableSlots = await _context.TimetableSlots
                .Include(ts => ts.Timetable)
                .Include(ts => ts.TeacherAssignment)
                    .ThenInclude(ta => ta.Teacher)
                .Include(ts => ts.TeacherAssignment)
                    .ThenInclude(ta => ta.Subject)
                .Include(ts => ts.TeacherAssignment)
                    .ThenInclude(ta => ta.Class)
                .Include(ts => ts.TeacherAssignment)
                    .ThenInclude(ta => ta.Section)
                .Where(ts => ts.DayOfWeek == currentDayOfWeek &&
                           !ts.IsBreak &&
                           !ts.IsZeroPeriod &&
                           ts.TeacherAssignmentId.HasValue)
                .ToListAsync();

            foreach (var slot in timetableSlots)
            {
                // Check if lecture ended more than 10 minutes ago
                var lectureEndTime = slot.EndTime;
                var tenMinutesAfterLecture = lectureEndTime.AddMinutes(10);

                if (currentTime < tenMinutesAfterLecture)
                    continue; // Lecture hasn't ended yet or 10 minutes haven't passed

                // Check if diary exists for today for this assignment
                var diaryExists = await _context.Diaries
                    .AnyAsync(d => d.TeacherAssignmentId == slot.TeacherAssignmentId &&
                                 d.Date == today);

                if (!diaryExists && slot.TeacherAssignment != null)
                {
                    // Check if we already sent a reminder today
                    var reminderSentToday = await _context.Notifications
                        .Where(n => n.Type == "reminder" &&
                                  n.RelatedEntityId == slot.TeacherAssignmentId &&
                                  n.RelatedEntityType == "TeacherAssignment" &&
                                  n.Timestamp.Date == today)
                        .AnyAsync();

                    if (!reminderSentToday)
                    {
                        await CreateDiaryReminderNotification(
                            slot.TeacherAssignment.TeacherId,
                            slot.TeacherAssignmentId.Value,
                            today,
                            slot.Timetable.CampusId
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Helper method to get admin user IDs for a specific campus
        /// </summary>
        private async Task<List<string>> GetAdminUserIdsForCampus(int campusId)
        {
            // Get admin users in this campus using a single optimized query
            var adminUserIds = await _context.Users
                .Where(u => u.CampusId == campusId && u.IsActive)
                .Join(_context.UserRoles,
                    u => u.Id,
                    ur => ur.UserId,
                    (u, ur) => new { User = u, ur.RoleId })
                .Join(_context.Roles.Where(r => r.Name == "Admin"),
                    uur => uur.RoleId,
                    r => r.Id,
                    (uur, r) => uur.User.Id)
                .Distinct()
                .ToListAsync();

            return adminUserIds;
        }
    }
}
