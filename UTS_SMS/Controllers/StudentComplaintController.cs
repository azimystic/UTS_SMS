using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using SMS.Services;
using System.Net;

namespace SMS.Controllers
{
    public class StudentComplaintController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly ILogger<StudentComplaintController> _logger;
        private readonly MessageService _messageService;

        public StudentComplaintController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService, IEmailService emailService, ILogger<StudentComplaintController> logger, MessageService messageService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _emailService = emailService;
            _logger = logger;
            _messageService = messageService;
        }

        // GET: StudentComplaint (For Students)
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Index(string tab = "open")
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.StudentId == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var student = await _context.Students
                .Include(s => s.Campus)
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .FirstOrDefaultAsync(s => s.Id == currentUser.StudentId);

            if (student == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            // Get outgoing complaints (complaints made by student)
            var outgoingComplaints = await _context.StudentComplaints
                .Where(sc => sc.StudentId == student.Id && sc.IsActive && sc.ReporterType == "Student")
                .OrderByDescending(sc => sc.ComplaintDate)
                .ToListAsync();

            // Get incoming complaints (complaints about student made by admin/teacher)
            var incomingComplaints = await _context.StudentComplaints
                .Where(sc => sc.StudentId == student.Id && sc.IsActive && 
                           (sc.ReporterType == "Admin" || sc.ReporterType == "Teacher"))
                .OrderByDescending(sc => sc.ComplaintDate)
                .ToListAsync();

            // Filter based on tab
            switch (tab.ToLower())
            {
                case "all":
                    // Show all complaints
                    break;
                case "resolved":
                    outgoingComplaints = outgoingComplaints.Where(c => c.Status == "Resolved").ToList();
                    incomingComplaints = incomingComplaints.Where(c => c.Status == "Resolved").ToList();
                    break;
                default: // "open"
                    outgoingComplaints = outgoingComplaints.Where(c => c.Status != "Resolved").ToList();
                    incomingComplaints = incomingComplaints.Where(c => c.Status != "Resolved").ToList();
                    break;
            }

            ViewBag.Student = student;
            ViewBag.OutgoingComplaints = outgoingComplaints;
            ViewBag.IncomingComplaints = incomingComplaints;
            ViewBag.CurrentTab = tab;
            
            return View();
        }

        // GET: StudentComplaint/Create (For Students)
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.StudentId == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var student = await _context.Students
                .Include(s => s.Campus)
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .FirstOrDefaultAsync(s => s.Id == currentUser.StudentId);

            if (student == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            // Get teachers and subjects assigned to this student's class and section
            var teacherAssignments = await _context.TeacherAssignments
                .Where(ta => ta.ClassId == student.Class && 
                           ta.SectionId == student.Section && 
                           ta.IsActive)
                .Include(ta => ta.Teacher)
                .Include(ta => ta.Subject)
                .ToListAsync();

            ViewBag.Student = student;
            ViewBag.Teachers = teacherAssignments.Select(ta => ta.Teacher).Distinct().ToList();
            ViewBag.Subjects = teacherAssignments.Select(ta => ta.Subject).Distinct().ToList();
            
            var complaint = new StudentComplaint
            {
                StudentId = student.Id,
                CampusId = student.CampusId,
                ComplaintDate = DateTime.Now,
                Status = "Open",
                ReporterType = "Student",
                ReportedBy = student.StudentName
            };

            return View(complaint);
        }

        // POST: StudentComplaint/Create (For Students)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Create([Bind("ComplaintTitle,ComplaintDescription,ComplaintType,Priority,TeacherId")] StudentComplaint studentComplaint)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.StudentId == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var student = await _context.Students
                .Include(s => s.Campus)
                .FirstOrDefaultAsync(s => s.Id == currentUser.StudentId);

            if (student == null)
            {
                return RedirectToAction("Logout", "Account");
            }
            ModelState.Remove("Campus");
            ModelState.Remove("Student");
            ModelState.Remove("Teacher");
            if (ModelState.IsValid)
            {
                studentComplaint.StudentId = student.Id;
                studentComplaint.CampusId = student.CampusId;
                studentComplaint.ComplaintDate = DateTime.Now;
                studentComplaint.Status = "Open";
                studentComplaint.ReporterType = "Student";
                studentComplaint.ReportedBy = student.StudentName;
                studentComplaint.CreatedBy = student.StudentName;
                studentComplaint.CreatedDate = DateTime.Now;
                studentComplaint.IsActive = true;

                _context.Add(studentComplaint);
                await _context.SaveChangesAsync();

                // Send notifications
                // 1. Notify Admin using the centralized notification service
                await _notificationService.CreateComplaintNotification(studentComplaint);

                // 2. Notify selected teacher if one was specified
                // Note: Teacher notifications are created separately as they have different
                // targeting requirements (role-based rather than admin-specific)
                if (studentComplaint.TeacherId.HasValue)
                {
                    var teacher = await _context.Employees.FindAsync(studentComplaint.TeacherId.Value);
                    if (teacher != null)
                    {
                        var teacherNotification = new Notification
                        {
                            Type = "complaint",
                            Title = "Student Complaint - Your Input Required",
                            Message = $"A complaint has been filed by {student.StudentName} that requires your attention. Type: {studentComplaint.ComplaintType}",
                            Timestamp = DateTime.Now,
                            TargetRole = "Teacher",
                            UserId = null, // Teachers will see this based on their role and TeacherId match
                            RelatedEntityId = studentComplaint.Id,
                            RelatedEntityType = "StudentComplaint",
                            ActionUrl = $"/StudentComplaint/Details/{studentComplaint.Id}",
                            CampusId = studentComplaint.CampusId,
                            CreatedBy = studentComplaint.CreatedBy
                        };
                        _context.Notifications.Add(teacherNotification);
                        await _context.SaveChangesAsync();
                    }
                }

                TempData["SuccessMessage"] = "Complaint submitted successfully. You will be notified of any updates.";
                return RedirectToAction(nameof(Index));
            }

            // Reload data for form
            var teacherAssignments = await _context.TeacherAssignments
                .Where(ta => ta.ClassId == student.Class && 
                           ta.SectionId == student.Section && 
                           ta.IsActive  )
                .Include(ta => ta.Teacher)
                .Include(ta => ta.Subject)
                .ToListAsync();

            ViewBag.Student = student;
            ViewBag.Teachers = teacherAssignments.Select(ta => ta.Teacher).Distinct().ToList();
            ViewBag.Subjects = teacherAssignments.Select(ta => ta.Subject).Distinct().ToList();

            return View(studentComplaint);
        }

        // GET: StudentComplaint/AdminIndex (For Admins)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminIndex(string tab = "incoming", string status = "open", string searchStudent = "", int? studentId = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            var query = _context.StudentComplaints
                .Include(sc => sc.Student)
                .ThenInclude(s => s.ClassObj)
                .Include(sc => sc.Student)
                .ThenInclude(s => s.SectionObj)
                .Include(sc => sc.Campus)
                .Include(sc => sc.Teacher)
                .Where(sc => sc.IsActive);

            // Filter by campus if user is not super admin
            if (campusId.HasValue)
            {
                query = query.Where(sc => sc.CampusId == campusId.Value);
            }

            // Filter by tab (incoming vs outgoing)
            if (tab == "outgoing")
            {
                // Outgoing = complaints created by admin
                query = query.Where(sc => sc.ReporterType == "Admin");
            }
            else
            {
                // Incoming = complaints from students or teachers
                query = query.Where(sc => sc.ReporterType == "Student" || sc.ReporterType == "Teacher");
            }

            // Filter by student if specified
            if (studentId.HasValue)
            {
                query = query.Where(sc => sc.StudentId == studentId.Value);
            }
            else if (!string.IsNullOrEmpty(searchStudent))
            {
                query = query.Where(sc => sc.Student.StudentName.Contains(searchStudent) ||
                                         sc.Student.FatherName.Contains(searchStudent));
            }

            // Filter by status if specified
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(sc => sc.Status == status);
            }

            var complaints = await query
                .OrderByDescending(sc => sc.ComplaintDate)
                .ToListAsync();

            // Get students for search dropdown
            var studentsQuery = _context.Students.Where(s => !s.HasLeft);
            if (campusId.HasValue)
            {
                studentsQuery = studentsQuery.Where(s => s.CampusId == campusId.Value);
            }
            ViewBag.Students = await studentsQuery.OrderBy(s => s.StudentName).ToListAsync();

            ViewBag.CurrentTab = tab;
            ViewBag.StatusFilter = status;
            ViewBag.SearchStudent = searchStudent;
            ViewBag.SelectedStudentId = studentId;
            ViewBag.Campuses = !campusId.HasValue 
                ? await _context.Campuses.Where(c => c.IsActive).ToListAsync() 
                : new List<Campus>();

            return View(complaints);
        }

        // POST: StudentComplaint/MarkAsResolved (For Admins)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkAsResolved(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var complaint = await _context.StudentComplaints.FindAsync(id);

            if (complaint == null)
            {
                return Json(new { success = false, message = "Complaint not found" });
            }

            // Check campus permissions
            if (currentUser?.CampusId.HasValue == true && complaint.CampusId != currentUser.CampusId.Value)
            {
                return Json(new { success = false, message = "Access denied" });
            }

            try
            {
                complaint.Status = "Resolved";
                complaint.ResolvedDate = DateTime.Now;
                complaint.ResolvedBy = currentUser?.FullName;
                complaint.ModifiedBy = currentUser?.FullName;
                complaint.ModifiedDate = DateTime.Now;
                
                // Add a note that it was marked as resolved
                if (!string.IsNullOrWhiteSpace(complaint.InvestigationNotes))
                {
                    complaint.InvestigationNotes += $"\n\nMarked as resolved by {currentUser?.FullName} on {DateTime.Now:MMM dd, yyyy 'at' hh:mm tt}";
                }
                else
                {
                    complaint.InvestigationNotes = $"Marked as resolved by {currentUser?.FullName} on {DateTime.Now:MMM dd, yyyy 'at' hh:mm tt}";
                }

                _context.Update(complaint);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Complaint marked as resolved successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating complaint status" });
            }
        }

        // POST: StudentComplaint/UpdateStatus (For Admins)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var complaint = await _context.StudentComplaints.FindAsync(id);

            if (complaint == null)
            {
                return Json(new { success = false, message = "Complaint not found" });
            }

            // Check campus permissions
            if (currentUser?.CampusId.HasValue == true && complaint.CampusId != currentUser.CampusId.Value)
            {
                return Json(new { success = false, message = "Access denied" });
            }

            try
            {
                complaint.Status = request.Status;
                complaint.ModifiedBy = currentUser?.FullName;
                complaint.ModifiedDate = DateTime.Now;
                
                if (request.Status == "Resolved")
                {
                    complaint.ResolvedDate = DateTime.Now;
                    complaint.ResolvedBy = currentUser?.FullName;
                }
                
                // Add a note about the status change
                var statusNote = $"Status changed to {request.Status} by {currentUser?.FullName} on {DateTime.Now:MMM dd, yyyy 'at' hh:mm tt}";
                if (!string.IsNullOrWhiteSpace(complaint.InvestigationNotes))
                {
                    complaint.InvestigationNotes += $"\n\n{statusNote}";
                }
                else
                {
                    complaint.InvestigationNotes = statusNote;
                }

                _context.Update(complaint);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Complaint marked as {request.Status} successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating complaint status" });
            }
        }

        // GET: StudentComplaint/Details (For Admins)
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var complaint = await _context.StudentComplaints
                .Include(sc => sc.Student)
                .ThenInclude(s => s.ClassObj)
                .Include(sc => sc.Student)
                .ThenInclude(s => s.SectionObj)
                .Include(sc => sc.Campus)
                .Include(sc => sc.Teacher)
                .FirstOrDefaultAsync(sc => sc.Id == id);

            if (complaint == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            
            // Check permissions
            if (User.IsInRole("Admin"))
            {
                if (currentUser?.CampusId.HasValue == true && complaint.CampusId != currentUser.CampusId.Value)
                    return Forbid();
            }
            else if (User.IsInRole("Teacher"))
            {
                if (complaint.TeacherId != currentUser?.EmployeeId)
                    return Forbid();
            }

            return View(complaint);
        }

        // GET: StudentComplaint/AdminCreate (For Admin and Teachers to register complaints for students)
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> AdminCreate(int? studentId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            // Get campuses (for admin) or just current campus (for teacher)
            var campuses = new List<Campus>();
            if (User.IsInRole("Admin") && !currentUser?.CampusId.HasValue == true)
            {
                campuses = await _context.Campuses.Where(c => c.IsActive).ToListAsync();
            }
            else if (currentUser?.CampusId.HasValue == true)
            {
                var campus = await _context.Campuses.FindAsync(currentUser.CampusId.Value);
                if (campus != null) campuses.Add(campus);
            }

            ViewBag.Campuses = new SelectList(campuses, "Id", "Name");
            ViewBag.Classes = new SelectList(Enumerable.Empty<Class>(), "Id", "Name");
            ViewBag.Sections = new SelectList(Enumerable.Empty<ClassSection>(), "Id", "Name");
            ViewBag.Students = new SelectList(Enumerable.Empty<Student>(), "Id", "StudentName");

            var complaint = new StudentComplaint
            {
                ComplaintDate = DateTime.Now,
                Status = "Open",
                ReporterType = User.IsInRole("Admin") ? "Admin" : "Teacher",
                ReportedBy = currentUser?.FullName ?? "Unknown",
                CampusId = currentUser?.CampusId ?? 0,
                StudentId = studentId ?? 0
            };

            if (studentId.HasValue)
            {
                var student = await _context.Students
                    .Include(s => s.ClassObj)
                    .Include(s => s.SectionObj)
                    .FirstOrDefaultAsync(s => s.Id == studentId.Value);
                
                if (student != null)
                {
                    ViewBag.SelectedStudent = student;
                    ViewBag.Classes = new SelectList(await _context.Classes.Where(c => c.IsActive && c.CampusId == student.CampusId).ToListAsync(), "Id", "Name", student.Class);
                    ViewBag.Sections = new SelectList(await _context.ClassSections.Where(cs => cs.IsActive && cs.ClassId == student.Class).ToListAsync(), "Id", "Name", student.Section);
                    ViewBag.Students = new SelectList(await _context.Students.Where(s => s.Class == student.Class && s.Section == student.Section).ToListAsync(), "Id", "StudentName", student.Id);
                }
            }

            return View(complaint);
        }

        // POST: StudentComplaint/AdminCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> AdminCreate([Bind("StudentId,CampusId,ComplaintTitle,ComplaintDescription,ComplaintType,Priority,RequiresParentNotification,IsAnonymous,TeacherId,TeacherComments")] StudentComplaint studentComplaint)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            ModelState.Remove("Student");
            ModelState.Remove("Campus");
            ModelState.Remove("ReportedBy");
            ModelState.Remove("ReporterType");
            ModelState.Remove("CreatedBy");
            ModelState.Remove("Teacher");

            if (ModelState.IsValid)
            {
                studentComplaint.ComplaintDate = DateTime.Now;
                studentComplaint.Status = "Open";
                studentComplaint.ReporterType = User.IsInRole("Admin") ? "Admin" : "Teacher";
                studentComplaint.ReportedBy = currentUser?.FullName ?? "Unknown";
                studentComplaint.CreatedBy = currentUser?.FullName ?? "Unknown";
                studentComplaint.CreatedDate = DateTime.Now;
                studentComplaint.IsActive = true;

                // Set teacher comment date if comments were provided
                if (!string.IsNullOrWhiteSpace(studentComplaint.TeacherComments))
                {
                    studentComplaint.TeacherCommentDate = DateTime.Now;
                }

                _context.Add(studentComplaint);
                await _context.SaveChangesAsync();

                // Send notifications
                // 1. Notify Admin (if complaint was created by teacher)
                if (User.IsInRole("Teacher"))
                {
                    await _notificationService.CreateComplaintNotification(studentComplaint);
                }

                // 2. Notify teacher if one was specified and they haven't added comments yet
                if (studentComplaint.TeacherId.HasValue && string.IsNullOrWhiteSpace(studentComplaint.TeacherComments))
                {
                    var teacher = await _context.Employees.FindAsync(studentComplaint.TeacherId.Value);
                    if (teacher != null)
                    {
                        var teacherNotification = new Notification
                        {
                            Type = "complaint",
                            Title = "Student Complaint - Your Input Required",
                            Message = $"A complaint has been registered that requires your attention. Complaint: {studentComplaint.ComplaintTitle}",
                            Timestamp = DateTime.Now,
                            TargetRole = "Teacher",
                            UserId = null, // Teachers will see this based on their role and TeacherId match
                            RelatedEntityId = studentComplaint.Id,
                            RelatedEntityType = "StudentComplaint",
                            ActionUrl = $"/StudentComplaint/Details/{studentComplaint.Id}",
                            CampusId = studentComplaint.CampusId,
                            CreatedBy = studentComplaint.CreatedBy
                        };
                        _context.Notifications.Add(teacherNotification);
                        await _context.SaveChangesAsync();
                    }
                }

                // 3. Send parent notification email if required
                if (studentComplaint.RequiresParentNotification)
                {
                    try
                    {
                        // Fetch student with family information
                        var student = await _context.Students
                            .Include(s => s.Family)
                            .FirstOrDefaultAsync(s => s.Id == studentComplaint.StudentId);

                        string parentEmail = null;
                        string parentName = "Parent/Guardian";

                        // Try to get parent email - prioritize Family email first (more likely to be parent), then student email as fallback
                        if (student != null)
                        {
                            // First try to get family email (most likely to be parent/guardian)
                            if (student.Family != null && !string.IsNullOrEmpty(student.Family.Email))
                            {
                                parentEmail = student.Family.Email;
                                parentName = student.Family.FatherName ?? "Parent/Guardian";
                            }
                            // Fallback to student email if family email not available
                            else if (!string.IsNullOrEmpty(student.Email))
                            {
                                parentEmail = student.Email;
                                parentName = student.FatherName ?? "Parent/Guardian";
                            }

                            // Send email if we have a valid email address
                            if (!string.IsNullOrEmpty(parentEmail))
                            {
                                var emailSubject = $"Student Complaint Notification - {studentComplaint.ComplaintTitle}";
                                var emailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #dc2626 0%, #991b1b 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .complaint-details {{ background: white; padding: 20px; border-left: 4px solid #dc2626; margin: 20px 0; }}
        .detail-row {{ margin: 10px 0; }}
        .label {{ font-weight: bold; color: #dc2626; }}
        .value {{ color: #333; }}
        .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>⚠️ Student Complaint Notification</h1>
            <p>School Management System</p>
        </div>
        <div class='content'>
            <h2>Dear {parentName},</h2>
            <p>We are writing to inform you that a complaint has been registered regarding your child/ward at our institution.</p>
            
            <div class='complaint-details'>
                <h3>Complaint Details</h3>
                <div class='detail-row'>
                    <span class='label'>Student:</span>
                    <span class='value'>{student.StudentName}</span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Title:</span>
                    <span class='value'>{studentComplaint.ComplaintTitle}</span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Type:</span>
                    <span class='value'>{studentComplaint.ComplaintType}</span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Priority:</span>
                    <span class='value'>{studentComplaint.Priority}</span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Date:</span>
                    <span class='value'>{studentComplaint.ComplaintDate:MMM dd, yyyy}</span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Reported By:</span>
                    <span class='value'>{studentComplaint.ReportedBy} ({studentComplaint.ReporterType})</span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Description:</span>
                    <div style='margin-top: 10px; padding: 10px; background: #f0f0f0; border-radius: 5px;'>
                        {studentComplaint.ComplaintDescription}
                    </div>
                </div>
            </div>

            <div class='warning'>
                <strong>📌 Action Required:</strong>
                <p>Please contact the school administration at your earliest convenience to discuss this matter. Your cooperation in resolving this issue is greatly appreciated.</p>
            </div>

            <p>If you have any questions or concerns, please do not hesitate to contact us.</p>
            
            <p><strong>Best regards,</strong><br/>School Administration</p>
        </div>
        <div class='footer'>
            <p>© {DateTime.Now.Year} School Management System. All rights reserved.</p>
            <p>This is an automated email. Please do not reply to this message.</p>
        </div>
    </div>
</body>
</html>";

                                var emailSent = await _emailService.SendEmailAsync(parentEmail, emailSubject, emailBody);
                                
                                if (emailSent)
                                {
                                    // Update complaint to mark parent as notified
                                    studentComplaint.ParentNotified = true;
                                    studentComplaint.ParentNotificationDate = DateTime.Now;
                                    await _context.SaveChangesAsync();
                                    
                                    _logger.LogInformation("Parent notification email sent successfully for complaint ID: {ComplaintId} to {Email}", 
                                        studentComplaint.Id, parentEmail);
                                }
                                else
                                {
                                    _logger.LogWarning("Failed to send parent notification email for complaint ID: {ComplaintId} to {Email}", 
                                        studentComplaint.Id, parentEmail);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Parent notification was required but no email found for student ID: {StudentId} (Complaint ID: {ComplaintId}). Student Email: {StudentEmail}, Family Email: {FamilyEmail}", 
                                    studentComplaint.StudentId, studentComplaint.Id, student.Email ?? "null", student.Family?.Email ?? "null");
                            }
                        }
                        else
                        {
                            _logger.LogError("Student not found for complaint ID: {ComplaintId}, Student ID: {StudentId}", 
                                studentComplaint.Id, studentComplaint.StudentId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending parent notification email for complaint ID: {ComplaintId}", studentComplaint.Id);
                        // Don't fail the entire complaint creation if email fails
                    }
                    
                    // 4. Send message to student via message service
                    try
                    {
                        // Get the student's user account
                        var studentUser = await _context.Users
                            .FirstOrDefaultAsync(u => u.StudentId == studentComplaint.StudentId);
                        
                        if (studentUser != null)
                        {
                            var messageSubject = $"Complaint Notification - {WebUtility.HtmlEncode(studentComplaint.ComplaintTitle)}";
                            var messageBody = $@"
<div style='font-family: Arial, sans-serif; padding: 20px;'>
    <h3 style='color: #dc2626;'>Student Complaint Notification</h3>
    <p>A complaint has been registered regarding you. Please review the details below:</p>
    
    <div style='background: #f9f9f9; padding: 15px; border-left: 4px solid #dc2626; margin: 15px 0;'>
        <p><strong>Title:</strong> {WebUtility.HtmlEncode(studentComplaint.ComplaintTitle)}</p>
        <p><strong>Type:</strong> {WebUtility.HtmlEncode(studentComplaint.ComplaintType)}</p>
        <p><strong>Priority:</strong> {WebUtility.HtmlEncode(studentComplaint.Priority)}</p>
        <p><strong>Date:</strong> {studentComplaint.ComplaintDate:MMM dd, yyyy}</p>
        <p><strong>Reported By:</strong> {WebUtility.HtmlEncode(studentComplaint.ReportedBy)} ({WebUtility.HtmlEncode(studentComplaint.ReporterType)})</p>
        <p><strong>Description:</strong></p>
        <p style='background: white; padding: 10px; border-radius: 5px;'>{WebUtility.HtmlEncode(studentComplaint.ComplaintDescription)}</p>
    </div>
    
    <p style='color: #666;'><strong>Important:</strong> The school administration may notify your parents/guardians about this complaint. Please take this matter seriously and work with the school administration to resolve it.</p>
    
    <p>If you have any questions, please contact the school administration.</p>
    
    <p style='margin-top: 20px;'><strong>School Administration</strong></p>
</div>";
                            
                            var recipientIds = new List<string> { studentUser.Id };
                            var messageId = await _messageService.SendMessage(
                                currentUser?.Id ?? "system",
                                messageSubject,
                                messageBody,
                                studentComplaint.CampusId,
                                recipientIds
                            );
                            
                            _logger.LogInformation("Student notification message sent successfully for complaint ID: {ComplaintId} to student user ID: {UserId}", 
                                studentComplaint.Id, studentUser.Id);
                        }
                        else
                        {
                            _logger.LogWarning("Student user account not found for student ID: {StudentId} (Complaint ID: {ComplaintId})", 
                                studentComplaint.StudentId, studentComplaint.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending student notification message for complaint ID: {ComplaintId}", studentComplaint.Id);
                        // Don't fail the entire complaint creation if message sending fails
                    }
                }
                
                TempData["SuccessMessage"] = "Complaint registered successfully.";
                return RedirectToAction(nameof(AdminIndex));
            }

            // Reload data for form if validation fails
            var campuses = new List<Campus>();
            if (User.IsInRole("Admin") && !currentUser?.CampusId.HasValue == true)
            {
                campuses = await _context.Campuses.Where(c => c.IsActive).ToListAsync();
            }
            else if (currentUser?.CampusId.HasValue == true)
            {
                var campus = await _context.Campuses.FindAsync(currentUser.CampusId.Value);
                if (campus != null) campuses.Add(campus);
            }

            ViewBag.Campuses = new SelectList(campuses, "Id", "Name", studentComplaint.CampusId);
            ViewBag.Classes = new SelectList(Enumerable.Empty<Class>(), "Id", "Name");
            ViewBag.Sections = new SelectList(Enumerable.Empty<ClassSection>(), "Id", "Name");
            ViewBag.Students = new SelectList(Enumerable.Empty<Student>(), "Id", "StudentName", studentComplaint.StudentId);

            return View(studentComplaint);
        }

        // GET: Get students by class and section (AJAX)
        [HttpGet]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<JsonResult> GetStudentsByClassSection(int classId, int sectionId)
        {
            var students = await _context.Students
                .Where(s => s.Class == classId && s.Section == sectionId && !s.HasLeft)
                .Select(s => new { Id = s.Id, StudentName = s.StudentName, StudentId = s.Id })
                .OrderBy(s => s.StudentName)
                .ToListAsync();

            return Json(students);
        }

        // GET: Get classes by campus (AJAX)
        [HttpGet]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<JsonResult> GetClassesByCampus(int campusId)
        {
            var classes = await _context.Classes
                .Where(c => c.IsActive && c.CampusId == campusId)
                .Select(c => new { Id = c.Id, Name = c.Name })
                .ToListAsync();

            return Json(classes);
        }

        // GET: Get sections by class (AJAX)
        [HttpGet]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<JsonResult> GetSectionsByClass(int classId)
        {
            var sections = await _context.ClassSections
                .Where(cs => cs.IsActive && cs.ClassId == classId)
                .Select(cs => new { Id = cs.Id, Name = cs.Name })
                .ToListAsync();

            return Json(sections);
        }

        // POST: StudentComplaint/SoftDelete (For Admins)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SoftDelete(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var complaint = await _context.StudentComplaints.FindAsync(id);

            if (complaint == null)
            {
                return Json(new { success = false, message = "Complaint not found" });
            }

            // Check campus permissions
            if (currentUser?.CampusId.HasValue == true && complaint.CampusId != currentUser.CampusId.Value)
            {
                return Json(new { success = false, message = "Access denied" });
            }

            try
            {
                complaint.IsActive = false;
                complaint.ModifiedBy = currentUser?.FullName;
                complaint.ModifiedDate = DateTime.Now;

                _context.Update(complaint);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Complaint deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting complaint" });
            }
        }

        // GET: Search students by name or roll number (AJAX)
        [HttpGet]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<JsonResult> SearchStudents(string term)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            var query = _context.Students.Where(s => !s.HasLeft);

            if (campusId.HasValue)
            {
                query = query.Where(s => s.CampusId == campusId.Value);
            }

            if (!string.IsNullOrWhiteSpace(term))
            {
                query = query.Where(s => s.StudentName.Contains(term) || 
                                        s.RollNumber.Contains(term) ||
                                        s.FatherName.Contains(term));
            }

            var students = await query
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .OrderBy(s => s.StudentName)
                .Take(20)
                .Select(s => new
                {
                    id = s.Id,
                    studentName = s.StudentName,
                    rollNumber = s.RollNumber,
                    className = s.ClassObj != null ? s.ClassObj.Name : "",
                    sectionName = s.SectionObj != null ? s.SectionObj.Name : "",
                    fatherName = s.FatherName
                })
                .ToListAsync();

            return Json(students);
        }

        // GET: Search teachers (AJAX)
        [HttpGet]
        [Authorize(Roles = "Student,Admin")]
        public async Task<JsonResult> SearchTeachers(string term, int? studentId = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var query = _context.Employees.Where(e => e.Role == "Teacher" && e.IsActive);

            if (!string.IsNullOrWhiteSpace(term))
            {
                query = query.Where(e => e.FullName.Contains(term));
            }

            var teachers = await query
                .OrderBy(e => e.FullName)
                .Take(20)
                .ToListAsync();

            // If studentId is provided, get their teacher assignments
            var teacherAssignments = new List<TeacherAssignment>();
            if (studentId.HasValue)
            {
                var student = await _context.Students.FindAsync(studentId.Value);
                if (student != null)
                {
                    teacherAssignments = await _context.TeacherAssignments
                        .Where(ta => ta.ClassId == student.Class && 
                                   ta.SectionId == student.Section && 
                                   ta.IsActive)
                        .Include(ta => ta.Subject)
                        .ToListAsync();
                }
            }

            var result = teachers.Select(t => new
            {
                id = t.Id,
                fullName = t.FullName,
                teaches = teacherAssignments.Any(ta => ta.TeacherId == t.Id),
                subjects = teacherAssignments
                    .Where(ta => ta.TeacherId == t.Id)
                    .Select(ta => ta.Subject?.Name ?? "")
                    .ToList()
            }).ToList();

            return Json(result);
        }

        // GET: StudentComplaint/TeacherIndex (For Teachers)
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> TeacherIndex(string status = "")
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.EmployeeId == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var query = _context.StudentComplaints
                .Include(sc => sc.Student)
                .ThenInclude(s => s.ClassObj)
                .Include(sc => sc.Student)
                .ThenInclude(s => s.SectionObj)
                .Include(sc => sc.Campus)
                .Where(sc => sc.IsActive && sc.TeacherId == currentUser.EmployeeId);

            // Filter by status if specified
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(sc => sc.Status == status);
            }

            var complaints = await query
                .OrderByDescending(sc => sc.ComplaintDate)
                .ToListAsync();

            ViewBag.StatusFilter = status;
            ViewBag.Teacher = await _context.Employees.FindAsync(currentUser.EmployeeId);

            return View(complaints);
        }

        // POST: StudentComplaint/AddTeacherComment
        [HttpPost]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> AddTeacherComment(int id, [FromBody] TeacherCommentRequest request)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.EmployeeId == null)
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var complaint = await _context.StudentComplaints.FindAsync(id);
            if (complaint == null)
            {
                return Json(new { success = false, message = "Complaint not found" });
            }

            if (complaint.TeacherId != currentUser.EmployeeId)
            {
                return Json(new { success = false, message = "You can only comment on complaints about you" });
            }

            try
            {
                complaint.TeacherComments = request.Comments;
                complaint.TeacherCommentDate = DateTime.Now;
                complaint.ModifiedBy = currentUser.FullName;
                complaint.ModifiedDate = DateTime.Now;

                _context.Update(complaint);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Your comment has been added successfully" });
            }
            catch (DbUpdateException dbEx)
            {
                // Log the exception
                Console.WriteLine($"Database error adding teacher comment: {dbEx.Message}");
                return Json(new { success = false, message = "Failed to save your comment. Please try again." });
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error adding teacher comment: {ex.Message}");
                return Json(new { success = false, message = "An unexpected error occurred while adding your comment." });
            }
        }

        // POST: StudentComplaint/ResolveComplaint (For Admins)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResolveComplaint(int id, [FromBody] ResolveComplaintRequest request)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var complaint = await _context.StudentComplaints.FindAsync(id);

            if (complaint == null)
            {
                return Json(new { success = false, message = "Complaint not found" });
            }

            // Check campus permissions
            if (currentUser?.CampusId.HasValue == true && complaint.CampusId != currentUser.CampusId.Value)
            {
                return Json(new { success = false, message = "Access denied" });
            }

            // Check if teacher has commented (if teacher is involved)
            if (complaint.TeacherId.HasValue && string.IsNullOrWhiteSpace(complaint.TeacherComments) && !request.OverrideTeacherComment)
            {
                return Json(new { 
                    success = false, 
                    message = "Teacher has not added comments yet. Please request teacher input before resolving.",
                    requiresOverride = true
                });
            }

            try
            {
                complaint.Status = "Resolved";
                complaint.ResolvedDate = DateTime.Now;
                complaint.ResolvedBy = currentUser?.FullName;
                complaint.ResolutionComments = request.ResolutionComments;
                complaint.ModifiedBy = currentUser?.FullName;
                complaint.ModifiedDate = DateTime.Now;

                _context.Update(complaint);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Complaint has been resolved successfully" });
            }
            catch (DbUpdateException dbEx)
            {
                // Log the exception
                Console.WriteLine($"Database error resolving complaint: {dbEx.Message}");
                return Json(new { success = false, message = "Failed to save resolution. Please try again." });
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error resolving complaint: {ex.Message}");
                return Json(new { success = false, message = "An unexpected error occurred while resolving the complaint." });
            }
        }

        private bool StudentComplaintExists(int id)
        {
            return _context.StudentComplaints.Any(e => e.Id == id);
        }

        // POST: StudentComplaint/SendTeacherReminder
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendTeacherReminder(int id)
        {
            var complaint = await _context.StudentComplaints
                .Include(sc => sc.Teacher)
                .FirstOrDefaultAsync(sc => sc.Id == id);

            if (complaint == null)
            {
                return Json(new { success = false, message = "Complaint not found" });
            }

            if (!complaint.TeacherId.HasValue)
            {
                return Json(new { success = false, message = "No teacher assigned to this complaint" });
            }

            try
            {
                var teacher = await _context.Employees.FindAsync(complaint.TeacherId.Value);
                if (teacher != null)
                {
                    var notification = new Notification
                    {
                        Type = "reminder",
                        Title = "Reminder: Add Comments to Student Complaint",
                        Message = $"Please add your comments to complaint #{complaint.Id} regarding {complaint.ComplaintTitle}. Admin is waiting to resolve this complaint.",
                        Timestamp = DateTime.Now,
                        TargetRole = "Teacher",
                        UserId = null, // Teachers will see this based on their role and TeacherId match
                        RelatedEntityId = complaint.Id,
                        RelatedEntityType = "StudentComplaint",
                        ActionUrl = $"/StudentComplaint/Details/{complaint.Id}",
                        CampusId = complaint.CampusId,
                        CreatedBy = User.Identity?.Name ?? "Admin"
                    };

                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = "Reminder sent to teacher successfully" });
                }

                return Json(new { success = false, message = "Teacher not found" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending reminder: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while sending the reminder" });
            }
        }

        // GET: StudentComplaint/GetTeacherComments
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTeacherComments(int id)
        {
            var complaint = await _context.StudentComplaints
                .Include(sc => sc.Teacher)
                .FirstOrDefaultAsync(sc => sc.Id == id);

            if (complaint == null)
            {
                return Json(new { success = false, message = "Complaint not found" });
            }

            return Json(new
            {
                success = true,
                hasTeacher = complaint.TeacherId.HasValue,
                teacherName = complaint.Teacher?.FullName ?? "",
                teacherComments = complaint.TeacherComments ?? "",
                teacherCommentDate = complaint.TeacherCommentDate?.ToString("MMM dd, yyyy 'at' hh:mm tt") ?? "",
                hasComments = !string.IsNullOrWhiteSpace(complaint.TeacherComments)
            });
        }
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    public class TeacherCommentRequest
    {
        public string Comments { get; set; } = string.Empty;
    }

    public class ResolveComplaintRequest
    {
        public string ResolutionComments { get; set; } = string.Empty;
        public bool OverrideTeacherComment { get; set; } = false;
    }
}