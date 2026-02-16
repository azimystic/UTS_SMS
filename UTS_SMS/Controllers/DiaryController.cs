// Controllers/DiaryController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UTS_SMS;
using UTS_SMS.Models;
using System.Security.Claims;

[Authorize]
public class DiaryController : Controller
{
    private readonly ApplicationDbContext _context; 
    private readonly UserManager<ApplicationUser> _userManager;

    public DiaryController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;

    }

    // GET: Diary
    public async Task<IActionResult> Index(DateTime? date)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var userCampusId = currentUser?.CampusId;
        var today = date ?? DateTime.Today;
     
        var userRole = User.IsInRole("Admin");
        var isOwner = User.IsInRole("Owner") || (userCampusId == null || userCampusId == 0);
        var isStudent = User.IsInRole("Student");
        var isTeacher = User.IsInRole("Teacher");

        IQueryable<TeacherAssignment> assignmentsQuery;

        if (isStudent)
        {
            // Student sees assignments for their class and section
            var studentId = currentUser.StudentId;
            if (studentId == null || studentId == 0)
                return Forbid();

            var student = await _context.Students
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                return Forbid();

            assignmentsQuery = _context.TeacherAssignments
                .Include(ta => ta.Teacher)
                .Include(ta => ta.Class)
                .Include(ta => ta.Section)
                .Include(ta => ta.Subject)
                .Where(ta => ta.ClassId == student.Class && ta.SectionId == student.Section);

            ViewBag.StudentClass = student.ClassObj?.Name;
            ViewBag.StudentSection = student.SectionObj?.Name;
        }
        else if (userRole)
        {
            // Admin sees assignments from their campus (or all if Owner)
            assignmentsQuery = _context.TeacherAssignments
                .Include(ta => ta.Teacher)
                .Include(ta => ta.Class)
                .Include(ta => ta.Section)
                .Include(ta => ta.Subject);
            
            // Filter by campus for non-owner admins
            if (!isOwner && userCampusId.HasValue && userCampusId.Value != 0)
            {
                assignmentsQuery = assignmentsQuery.Where(ta => ta.CampusId == userCampusId.Value);
            }
        }
        else if (isTeacher)
        {
            // Teacher sees only their own
            var teacherId = currentUser.EmployeeId;
            if (teacherId == 0)
                return Forbid();

            assignmentsQuery = _context.TeacherAssignments
                .Where(ta => ta.TeacherId == teacherId)
                .Include(ta => ta.Teacher)
                .Include(ta => ta.Class)
                .Include(ta => ta.Section)
                .Include(ta => ta.Subject);
        }
        else
        {
            return Forbid();
        }

        // Get distinct classes from assignments
        var classes = await assignmentsQuery
            .Select(ta => ta.Class)
            .Distinct()
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.SelectedDate = today.ToString("yyyy-MM-dd");
        ViewBag.Today = DateTime.Today.ToString("yyyy-MM-dd");
        ViewBag.IsAdmin = userRole;
        ViewBag.IsOwner = isOwner;
        ViewBag.IsStudent = isStudent;
        ViewBag.IsTeacher = isTeacher;

        return View(classes);
    }

    // GET: Diary/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var diary = await _context.Diaries
            .Include(d => d.TeacherAssignment)
                .ThenInclude(ta => ta.Teacher)
            .Include(d => d.TeacherAssignment)
                .ThenInclude(ta => ta.Class)
            .Include(d => d.TeacherAssignment)
                .ThenInclude(ta => ta.Section)
            .Include(d => d.TeacherAssignment)
                .ThenInclude(ta => ta.Subject)
            .Include(d => d.DiaryImages)
            .Include(d => d.Chapter)
                .ThenInclude(c => c.Subject)
            .Include(d => d.ChapterSection)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (diary == null)
            return NotFound();

        // Order images in memory after loading
        if (diary.DiaryImages != null && diary.DiaryImages.Any())
        {
            diary.DiaryImages = diary.DiaryImages.OrderBy(di => di.DisplayOrder).ToList();
        }

        var currentUser = await _userManager.GetUserAsync(User);
        var isAdmin = User.IsInRole("Admin");
        var isTeacher = User.IsInRole("Teacher");
        var isStudent = User.IsInRole("Student");

        // Authorization check for students
        if (isStudent)
        {
            var studentId = currentUser.StudentId;
            if (studentId == null || studentId == 0)
                return Forbid();

            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return Forbid();

            // Student can only view diaries for their class and section
            if (diary.TeacherAssignment.ClassId != student.Class || 
                diary.TeacherAssignment.SectionId != student.Section)
                return Forbid();
        }
        
        ViewBag.IsAdmin = isAdmin;
        ViewBag.IsTeacher = isTeacher;
        ViewBag.IsStudent = isStudent;
        
        // Check if current user is the owner (for teachers)
        var teacherId = currentUser.EmployeeId ?? 0;
        ViewBag.IsOwner = isTeacher && diary.TeacherAssignment.TeacherId == teacherId;
        ViewBag.TeacherId = teacherId;
        
        // Check if teacher has a diary entry for today
        var today = DateTime.Today;
        var hasTodayDiary = false;
        if (isTeacher && teacherId > 0)
        {
            hasTodayDiary = await _context.Diaries
                .AnyAsync(d => d.TeacherAssignment.TeacherId == teacherId && 
                              d.Date == today);
        }
        ViewBag.HasTodayDiary = hasTodayDiary;

        return View(diary);
    }

    // GET: Diary/Create
    public async Task<IActionResult> Create(DateTime? date, int? classId, int? sectionId, int? subjectId)
    {
        var today = date ?? DateTime.Today;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var currentUser = await _userManager.GetUserAsync(User);
        var teacher = await _context.Employees.FindAsync(currentUser.EmployeeId);
        var teacherId = currentUser.EmployeeId;
        // Get TeacherId from Teacher table using ApplicationUserId


        if (teacherId == 0 && !User.IsInRole("Admin"))
            return Forbid();

        var query = _context.TeacherAssignments.AsQueryable();

        if (!User.IsInRole("Admin"))
            query = query.Where(ta => ta.TeacherId == teacherId);

        // Filter by class, section, and subject if provided
        if (classId.HasValue && classId > 0)
            query = query.Where(ta => ta.ClassId == classId);
        
        if (sectionId.HasValue && sectionId > 0)
            query = query.Where(ta => ta.SectionId == sectionId);
        
        if (subjectId.HasValue && subjectId > 0)
            query = query.Where(ta => ta.SubjectId == subjectId);

        var assignments = await query
            .Include(ta => ta.Class)
            .Include(ta => ta.Section)
            .Include(ta => ta.Subject)
            .Select(ta => new
            {
                Id = ta.Id,
                Display = $"{ta.Class.Name}-{ta.Section.Name}-{ta.Subject.Name}",
                ta.ClassId,
                ta.SectionId,
                ta.SubjectId
            })
            .ToListAsync();

        // Pre-select the assignment if only one matches the filters
        int? selectedAssignmentId = null;
        if (assignments.Count == 1)
        {
            selectedAssignmentId = assignments[0].Id;
        }
        else if (classId.HasValue && sectionId.HasValue && subjectId.HasValue)
        {
            // Find exact match
            var exactMatch = assignments.FirstOrDefault(a => 
                a.ClassId == classId && 
                a.SectionId == sectionId && 
                a.SubjectId == subjectId);
            if (exactMatch != null)
                selectedAssignmentId = exactMatch.Id;
        }

        ViewBag.TeacherAssignmentId = new SelectList(
            assignments,
            "Id",
            "Display",
            selectedAssignmentId
        );

        ViewBag.Date = today.ToString("yyyy-MM-dd");
        
        // Get chapters for optional linking, filtered by subject if provided
        var chaptersQuery = _context.Chapters.Where(c => c.IsActive);
        
        if (subjectId.HasValue && subjectId > 0)
            chaptersQuery = chaptersQuery.Where(c => c.SubjectId == subjectId);
        
        ViewBag.Chapters = new SelectList(
            await chaptersQuery
                .Include(c => c.Subject)
                .OrderBy(c => c.Subject.Name)
                .ThenBy(c => c.ChapterNumber)
                .Select(c => new { c.Id, Display = $"{c.Subject.Name} - Ch {c.ChapterNumber}: {c.Name}" })
                .ToListAsync(),
            "Id",
            "Display"
        );

        return View();
    }

    // POST: Diary/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Diary diary, DateTime date, List<IFormFile> images)
    {
        diary.Date = date;

        var existingDiary = await _context.Diaries
            .AnyAsync(d => d.TeacherAssignmentId == diary.TeacherAssignmentId && d.Date == diary.Date);

        if (existingDiary)
        {
            ModelState.AddModelError("", "A diary already exists for this assignment on the selected date.");
        }
        var currentUser = await _userManager.GetUserAsync(User);
        ModelState.Remove("TeacherAssignment");
        ModelState.Remove("CreatedBy");
        ModelState.Remove("ModifiedBy");
        ModelState.Remove("Campus");
        ModelState.Remove("images");
        ModelState.Remove("Chapter");
        ModelState.Remove("ChapterSection");
        if (!ModelState.IsValid)
        {
            // Re-populate ViewBag
      
            var teacher = await _context.Employees.FindAsync(currentUser.EmployeeId);
            var teacherId = currentUser.EmployeeId;

            var query = _context.TeacherAssignments.AsQueryable();
            if (!User.IsInRole("Admin"))
                query = query.Where(ta => ta.TeacherId == teacherId);

            var assignments = await query
                .Include(ta => ta.Class)
                .Include(ta => ta.Section)
                .Include(ta => ta.Subject)
                .Select(ta => new
                {
                    Id = ta.Id,
                    Display = $"{ta.Class.Name}-{ta.Section.Name}-{ta.Subject.Name}"
                })
                .ToListAsync();

            ViewBag.TeacherAssignmentId = new SelectList(assignments, "Id", "Display");
            ViewBag.Date = date.ToString("yyyy-MM-dd");
            return View(diary);
        }

        var userName = User.Identity.Name;
        diary.CreatedBy = userName;
        diary.CampusId = (int)currentUser.CampusId;
        diary.CreatedAt = DateTime.Now;
        diary.ModifiedBy = "";
        
        _context.Diaries.Add(diary);
        await _context.SaveChangesAsync();

        // Handle image uploads
        if (images != null && images.Any())
        {
            await UploadDiaryImages(diary.Id, images, userName);
        }

        TempData["SuccessMessage"] = "Diary entry created successfully!";
        return RedirectToAction(nameof(Index), new { date = diary.Date.ToString("yyyy-MM-dd") });
    }

    // GET: Diary/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var diary = await _context.Diaries
            .Include(d => d.TeacherAssignment)
            .ThenInclude(ta => ta.Class)
            .Include(d => d.TeacherAssignment)
            .ThenInclude(ta => ta.Section)
            .Include(d => d.TeacherAssignment)
            .ThenInclude(ta => ta.Subject)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (diary == null)
            return NotFound();

        // Authorization: Only owner (teacher) or admin can edit
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isTeacher = User.IsInRole("Teacher");
        var isAdmin = User.IsInRole("Admin");
        var currentUser = await _userManager.GetUserAsync(User);
        var teacher = await _context.Employees.FindAsync(currentUser.EmployeeId);
        var teacherId = currentUser.EmployeeId;
        if (isTeacher)
        {
            

            if (diary.TeacherAssignment.TeacherId != teacherId)
                return Forbid();
        }

        ViewBag.Date = diary.Date.ToString("yyyy-MM-dd");
        
        // Load images for the diary
        ViewBag.DiaryImages = await _context.DiaryImages
            .Where(di => di.DiaryId == id)
            .OrderBy(di => di.DisplayOrder)
            .ToListAsync();
        
        // Get chapters for optional linking
        ViewBag.Chapters = new SelectList(
            await _context.Chapters
                .Where(c => c.IsActive && c.SubjectId == diary.TeacherAssignment.SubjectId)
                .OrderBy(c => c.ChapterNumber)
                .Select(c => new { c.Id, Display = $"Ch {c.ChapterNumber}: {c.Name}" })
                .ToListAsync(),
            "Id",
            "Display",
            diary.ChapterId
        );
        
        // Get sections for the selected chapter
        if (diary.ChapterId.HasValue)
        {
            ViewBag.ChapterSections = new SelectList(
                await _context.ChapterSections
                    .Where(cs => cs.ChapterId == diary.ChapterId && cs.IsActive)
                    .OrderBy(cs => cs.DisplayOrder)
                    .ToListAsync(),
                "Id",
                "Name",
                diary.ChapterSectionId
            );
        }
            
        return View(diary);
    }

    // POST: Diary/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Diary diary, DateTime date, List<IFormFile> images)
    {
        if (id != diary.Id) return NotFound();

        var existingDiary = await _context.Diaries.FindAsync(id);
        if (existingDiary == null) return NotFound();

        // Re-check uniqueness (except self)
        var duplicate = await _context.Diaries
            .AnyAsync(d => d.Id != id &&
                           d.TeacherAssignmentId == diary.TeacherAssignmentId &&
                           d.Date == date);

        if (duplicate)
        {
            ModelState.AddModelError("", "Another diary exists for this assignment on this date.");
        }
        ModelState.Remove("TeacherAssignment");
        ModelState.Remove("CreatedBy");
        ModelState.Remove("Campus");
        ModelState.Remove("ModifiedBy");
        if (!ModelState.IsValid)
        {
            ViewBag.Date = date.ToString("yyyy-MM-dd");
            // Load images for the view
            ViewBag.DiaryImages = await _context.DiaryImages
                .Where(di => di.DiaryId == id)
                .OrderBy(di => di.DisplayOrder)
                .ToListAsync();
            return View(diary);
        }

        existingDiary.LessonSummary = diary.LessonSummary;
        existingDiary.HomeworkGiven = diary.HomeworkGiven;
        existingDiary.Notes = diary.Notes;
        existingDiary.ChapterId = diary.ChapterId;
        existingDiary.ChapterSectionId = diary.ChapterSectionId;
        existingDiary.ModifiedAt = DateTime.Now;
        existingDiary.ModifiedBy = User.Identity.Name;

        _context.Update(existingDiary);

        // Handle new image uploads
        if (images != null && images.Any())
        {
            await UploadDiaryImages(id, images, User.Identity.Name);
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Diary updated successfully!";
        return RedirectToAction(nameof(Index), new { date = diary.Date.ToString("yyyy-MM-dd") });
    }

    // POST: Diary/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var diary = await _context.Diaries.FindAsync(id);
        if (diary == null) return NotFound();

        // Authorization
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isTeacher = User.IsInRole("Teacher");
        var currentUser = await _userManager.GetUserAsync(User);
        var teacher = await _context.Employees.FindAsync(currentUser.EmployeeId);
        var teacherId = currentUser.EmployeeId;
        if (isTeacher)
        {
            

            if (diary.TeacherAssignment.TeacherId != teacherId)
                return Forbid();
        }

        _context.Diaries.Remove(diary);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Diary deleted successfully!";
        return RedirectToAction(nameof(Index), new { date = diary.Date.ToString("yyyy-MM-dd") });
    }

    // Helper method to upload multiple images
    private async Task UploadDiaryImages(int diaryId, List<IFormFile> images, string? uploadedBy)
    {
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "diary");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var displayOrder = await _context.DiaryImages
            .Where(di => di.DiaryId == diaryId)
            .CountAsync() + 1;

        foreach (var image in images)
        {
            if (image.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                var fileExtension = Path.GetExtension(image.FileName).ToLower();
                
                if (!allowedExtensions.Contains(fileExtension))
                    continue;

                var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                var diaryImage = new DiaryImage
                {
                    DiaryId = diaryId,
                    ImagePath = Path.Combine("uploads", "diary", uniqueFileName).Replace("\\", "/"),
                    OriginalFileName = image.FileName,
                    DisplayOrder = displayOrder++,
                    UploadedBy = uploadedBy,
                    UploadedAt = DateTime.Now
                };

                _context.DiaryImages.Add(diaryImage);
            }
        }

        await _context.SaveChangesAsync();
    }

    // POST: Diary/DeleteImage/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int id)
    {
        var diaryImage = await _context.DiaryImages
            .Include(di => di.Diary)
            .FirstOrDefaultAsync(di => di.Id == id);

        if (diaryImage == null)
            return NotFound();

        // Authorization check
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isTeacher = User.IsInRole("Teacher");
        var currentUser = await _userManager.GetUserAsync(User);
        var teacherId = currentUser.EmployeeId;
        
        if (isTeacher)
        {
            var teacherAssignment = await _context.TeacherAssignments
                .FirstOrDefaultAsync(ta => ta.Id == diaryImage.Diary.TeacherAssignmentId);
                
            if (teacherAssignment?.TeacherId != teacherId)
                return Forbid();
        }

        // Delete physical file
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", diaryImage.ImagePath);
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }

        _context.DiaryImages.Remove(diaryImage);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Image deleted successfully!";
        return RedirectToAction(nameof(Edit), new { id = diaryImage.DiaryId });
    }
    
    // GET: API endpoint to get sections for a class
    [HttpGet]
    public async Task<IActionResult> GetSectionsByClass(int classId, DateTime date)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var userCampusId = currentUser?.CampusId;
        var isAdmin = User.IsInRole("Admin");
        var isOwner = User.IsInRole("Owner") || (userCampusId == null || userCampusId == 0);
        var isStudent = User.IsInRole("Student");
        var isTeacher = User.IsInRole("Teacher");
        
        IQueryable<TeacherAssignment> query = _context.TeacherAssignments
            .Include(ta => ta.Section)
            .Where(ta => ta.ClassId == classId);
        
        // Apply role-based filtering
        if (isStudent)
        {
            var studentId = currentUser.StudentId;
            if (studentId == null || studentId == 0)
                return Json(new List<object>());

            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return Json(new List<object>());

            query = query.Where(ta => ta.SectionId == student.Section);
        }
        else if (isTeacher)
        {
            var teacherId = currentUser.EmployeeId;
            query = query.Where(ta => ta.TeacherId == teacherId);
        }
        else if (isAdmin && !isOwner && userCampusId.HasValue && userCampusId.Value != 0)
        {
            query = query.Where(ta => ta.CampusId == userCampusId.Value);
        }
        
        var sections = await query
            .Select(ta => ta.Section)
            .Distinct()
            .OrderBy(s => s.Name)
            .ToListAsync();
        
        return Json(sections.Select(s => new { id = s.Id, name = s.Name }));
    }
    
    // GET: API endpoint to get subjects for a section
    [HttpGet]
    public async Task<IActionResult> GetSubjectsBySection(int classId, int sectionId, DateTime date)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var userCampusId = currentUser?.CampusId;
        var isAdmin = User.IsInRole("Admin");
        var isOwner = User.IsInRole("Owner") || (userCampusId == null || userCampusId == 0);
        var isStudent = User.IsInRole("Student");
        var isTeacher = User.IsInRole("Teacher");
        
        IQueryable<TeacherAssignment> query = _context.TeacherAssignments
            .Include(ta => ta.Subject)
            .Include(ta => ta.Teacher)
            .Where(ta => ta.ClassId == classId && ta.SectionId == sectionId);
        
        // Apply role-based filtering
        if (isStudent)
        {
            var studentId = currentUser.StudentId;
            if (studentId == null || studentId == 0)
                return Json(new List<object>());

            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return Json(new List<object>());

            // Student can only see subjects for their own class and section
            if (student.Class != classId || student.Section != sectionId)
                return Json(new List<object>());
        }
        else if (isTeacher)
        {
            var teacherId = currentUser.EmployeeId;
            query = query.Where(ta => ta.TeacherId == teacherId);
        }
        else if (isAdmin && !isOwner && userCampusId.HasValue && userCampusId.Value != 0)
        {
            query = query.Where(ta => ta.CampusId == userCampusId.Value);
        }
        
        var assignments = await query.ToListAsync();
        
        // Get diaries for this date
        var diaries = await _context.Diaries
            .Include(d => d.DiaryImages)
            .Where(d => d.Date == date && assignments.Select(a => a.Id).Contains(d.TeacherAssignmentId))
            .ToListAsync();
        
        var result = assignments.Select(a => {
            var diary = diaries.FirstOrDefault(d => d.TeacherAssignmentId == a.Id);
            return new
            {
                assignmentId = a.Id,
                subjectId = a.SubjectId,
                subjectName = a.Subject.Name,
                teacherName = a.Teacher.FullName,
                hasDiary = diary != null,
                diaryId = diary?.Id,
                lessonSummary = diary?.LessonSummary,
                homework = diary?.HomeworkGiven,
                imageCount = diary?.DiaryImages?.Count ?? 0
            };
        }).ToList();
        
        return Json(result);
    }
}