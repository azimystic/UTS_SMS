using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SMS;
using SMS.Models;
using SMS.ViewModels;
using System.Security.Claims;

namespace SMS.Controllers
{
    [Authorize]
    public class AcademicMaterialController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public AcademicMaterialController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        // GET: AcademicMaterial/Index - List all classes
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isTeacher = User.IsInRole("Teacher");

            IQueryable<Class> classesQuery = _context.Classes.Where(c => c.IsActive);

            if (!isAdmin && isTeacher)
            {
                // Teacher sees only their assigned classes
                var teacherAssignments = await _context.TeacherAssignments
                    .Where(ta => ta.TeacherId == currentUser.EmployeeId && ta.IsActive)
                    .Select(ta => ta.ClassId)
                    .Distinct()
                    .ToListAsync();

                classesQuery = classesQuery.Where(c => teacherAssignments.Contains(c.Id));
            }

            if (currentUser.CampusId.HasValue && currentUser.CampusId > 0)
            {
                classesQuery = classesQuery.Where(c => c.CampusId == currentUser.CampusId);
            }

            var classes = await classesQuery
                .Include(c => c.Campus)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(classes);
        }

        // GET: AcademicMaterial/Subjects/5 - List subjects for a class
        public async Task<IActionResult> Subjects(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isTeacher = User.IsInRole("Teacher");

            var classEntity = await _context.Classes.FindAsync(id);
            if (classEntity == null || !classEntity.IsActive)
                return NotFound();

            // Check authorization
            if (!isAdmin && isTeacher)
            {
                var hasAccess = await _context.TeacherAssignments
                    .AnyAsync(ta => ta.TeacherId == currentUser.EmployeeId && ta.ClassId == id && ta.IsActive);
                if (!hasAccess)
                    return Forbid();
            }

            IQueryable<Subject> subjectsQuery = _context.TeacherAssignments
                .Where(ta => ta.ClassId == id && ta.IsActive)
                .Select(ta => ta.Subject);

            // Filter subjects for teachers based on their assignments
            if (!isAdmin && isTeacher && currentUser.EmployeeId.HasValue)
            {
                subjectsQuery = _context.TeacherAssignments
                    .Where(ta => ta.TeacherId == currentUser.EmployeeId.Value && 
                                ta.ClassId == id && 
                                ta.IsActive)
                    .Select(ta => ta.Subject);
            }

            var subjects = await subjectsQuery
                .Distinct()
                .ToListAsync();

            ViewBag.Class = classEntity;
            return View(subjects);
        }

        // GET: AcademicMaterial/Chapters/5 - List chapters for a subject
        public async Task<IActionResult> Chapters(int id, int classId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isTeacher = User.IsInRole("Teacher");

            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null || !subject.IsActive)
                return NotFound();

            var classEntity = await _context.Classes.FindAsync(classId);
            if (classEntity == null)
                return NotFound();

            // Check authorization
            if (!isAdmin && isTeacher)
            {
                var hasAccess = await _context.TeacherAssignments
                    .AnyAsync(ta => ta.TeacherId == currentUser.EmployeeId && 
                                   ta.SubjectId == id && 
                                   ta.ClassId == classId && 
                                   ta.IsActive);
                if (!hasAccess)
                    return Forbid();
            }

            var chapters = await _context.Chapters
                .Where(c => c.SubjectId == id && c.IsActive)
                .Include(c => c.Subject)
                .OrderBy(c => c.ChapterNumber)
                .ToListAsync();

            ViewBag.Subject = subject;
            ViewBag.Class = classEntity;
            return View(chapters);
        }

        // GET: AcademicMaterial/Create
        public async Task<IActionResult> Create(int subjectId, int classId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isTeacher = User.IsInRole("Teacher");

            var subject = await _context.Subjects.FindAsync(subjectId);
            if (subject == null)
                return NotFound();

            // Check authorization
            if (!isAdmin && isTeacher)
            {
                var hasAccess = await _context.TeacherAssignments
                    .AnyAsync(ta => ta.TeacherId == currentUser.EmployeeId && 
                                   ta.SubjectId == subjectId && 
                                   ta.ClassId == classId && 
                                   ta.IsActive);
                if (!hasAccess)
                    return Forbid();
            }

            var model = new AcademicMaterialViewModel
            {
                SubjectId = subjectId,
                Subject = subject
            };

            ViewBag.ClassId = classId;
            return View(model);
        }

        // POST: AcademicMaterial/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AcademicMaterialViewModel model, int classId, List<IFormFile> materialFiles)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isTeacher = User.IsInRole("Teacher");

            // Check authorization
            if (!isAdmin && isTeacher)
            {
                var hasAccess = await _context.TeacherAssignments
                    .AnyAsync(ta => ta.TeacherId == currentUser.EmployeeId && 
                                   ta.SubjectId == model.SubjectId && 
                                   ta.ClassId == classId && 
                                   ta.IsActive);
                if (!hasAccess)
                    return Forbid();
            }

            // Remove navigation properties from validation
            ModelState.Remove("Subject");
            
            if (!ModelState.IsValid)
            {
                model.Subject = await _context.Subjects.FindAsync(model.SubjectId);
                ViewBag.ClassId = classId;
                return View(model);
            }

            // Create Chapter
            var chapter = new Chapter
            {
                SubjectId = model.SubjectId,
                Name = model.ChapterName,
                ChapterNumber = model.ChapterNumber,
                Description = model.ChapterDescription,
                CreatedAt = DateTime.Now,
                CreatedBy = User.Identity.Name,
                CampusId = currentUser.CampusId ?? 0,
                IsActive = true
            };

            _context.Chapters.Add(chapter);
            await _context.SaveChangesAsync();

            // Create Questions (Sections removed - no longer supported)
            foreach (var question in model.Questions.Where(q => !q.IsDeleted))
            {
                var questionEntity = new Question
                {
                    ChapterId = chapter.Id,
                    ChapterSectionId = null, // No longer using sections
                    Type = question.Type,
                    QuestionText = question.QuestionText,
                    Answer = question.Answer,
                    OptionA = question.OptionA,
                    OptionB = question.OptionB,
                    OptionC = question.OptionC,
                    OptionD = question.OptionD,
                    CorrectOption = question.CorrectOption,
                    DisplayOrder = question.DisplayOrder,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };
                _context.Questions.Add(questionEntity);
            }

            // Handle file uploads
            if (materialFiles != null && materialFiles.Any())
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "academic-materials");
                Directory.CreateDirectory(uploadsFolder);

                for (int i = 0; i < materialFiles.Count; i++)
                {
                    var file = materialFiles[i];
                    if (file?.Length > 0 && i < model.Materials.Count)
                    {
                        var materialViewModel = model.Materials[i];
                        if (materialViewModel.IsDeleted) continue;

                        var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }

                        var material = new ChapterMaterial
                        {
                            ChapterId = chapter.Id,
                            ChapterSectionId = null, // No longer using sections
                            Type = materialViewModel.Type,
                            Heading = materialViewModel.Heading,
                            Description = materialViewModel.Description,
                            FilePath = Path.Combine("uploads", "academic-materials", uniqueFileName).Replace("\\", "/"),
                            OriginalFileName = file.FileName,
                            DisplayOrder = materialViewModel.DisplayOrder,
                            UploadedAt = DateTime.Now,
                            UploadedBy = User.Identity.Name,
                            IsActive = true
                        };
                        _context.ChapterMaterials.Add(material);
                    }
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Chapter created successfully!";
            return RedirectToAction(nameof(Chapters), new { id = model.SubjectId, classId = classId });
        }

        // GET: AcademicMaterial/Edit/5
        public async Task<IActionResult> Edit(int id, int classId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isTeacher = User.IsInRole("Teacher");

            var chapter = await _context.Chapters
                .Include(c => c.Subject)
                .Include(c => c.ChapterSections)
                .Include(c => c.Questions)
                .Include(c => c.ChapterMaterials)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (chapter == null)
                return NotFound();

            // Check authorization
            if (!isAdmin && isTeacher)
            {
                var hasAccess = await _context.TeacherAssignments
                    .AnyAsync(ta => ta.TeacherId == currentUser.EmployeeId && 
                                   ta.SubjectId == chapter.SubjectId && 
                                   ta.ClassId == classId && 
                                   ta.IsActive);
                if (!hasAccess)
                    return Forbid();
            }

            var model = new AcademicMaterialViewModel
            {
                ChapterId = chapter.Id,
                SubjectId = chapter.SubjectId,
                ChapterName = chapter.Name,
                ChapterNumber = chapter.ChapterNumber,
                ChapterDescription = chapter.Description,
                Subject = chapter.Subject,
                Sections = chapter.ChapterSections.Select(s => new ChapterSectionViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    DisplayOrder = s.DisplayOrder
                }).ToList(),
                Questions = chapter.Questions.Select(q => new QuestionViewModel
                {
                    Id = q.Id,
                    SectionId = q.ChapterSectionId,
                    Type = q.Type,
                    QuestionText = q.QuestionText,
                    Answer = q.Answer,
                    OptionA = q.OptionA,
                    OptionB = q.OptionB,
                    OptionC = q.OptionC,
                    OptionD = q.OptionD,
                    CorrectOption = q.CorrectOption,
                    DisplayOrder = q.DisplayOrder
                }).ToList(),
                ExistingMaterials = chapter.ChapterMaterials.ToList()
            };

            ViewBag.ClassId = classId;
            return View(model);
        }

        // POST: AcademicMaterial/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AcademicMaterialViewModel model, int classId, List<IFormFile> materialFiles)
        {
            if (id != model.ChapterId)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isTeacher = User.IsInRole("Teacher");

            var chapter = await _context.Chapters
                .Include(c => c.ChapterSections)
                .Include(c => c.Questions)
                .Include(c => c.ChapterMaterials)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (chapter == null)
                return NotFound();

            // Check authorization
            if (!isAdmin && isTeacher)
            {
                var hasAccess = await _context.TeacherAssignments
                    .AnyAsync(ta => ta.TeacherId == currentUser.EmployeeId && 
                                   ta.SubjectId == chapter.SubjectId && 
                                   ta.ClassId == classId && 
                                   ta.IsActive);
                if (!hasAccess)
                    return Forbid();
            }

            ModelState.Remove("Subject");

            if (!ModelState.IsValid)
            {
                model.Subject = await _context.Subjects.FindAsync(model.SubjectId);
                model.ExistingMaterials = chapter.ChapterMaterials.ToList();
                ViewBag.ClassId = classId;
                return View(model);
            }

            // Update Chapter
            chapter.Name = model.ChapterName;
            chapter.ChapterNumber = model.ChapterNumber;
            chapter.Description = model.ChapterDescription;
            chapter.ModifiedAt = DateTime.Now;
            chapter.ModifiedBy = User.Identity.Name;

            // No sections anymore - removed for simplification

            // Update Questions
            var existingQuestionIds = chapter.Questions.Select(q => q.Id).ToList();
            var updatedQuestionIds = model.Questions.Where(q => q.Id.HasValue && !q.IsDeleted).Select(q => q.Id.Value).ToList();
            
            // Remove deleted questions
            var questionsToRemove = chapter.Questions.Where(q => !updatedQuestionIds.Contains(q.Id)).ToList();
            _context.Questions.RemoveRange(questionsToRemove);

            // Update or add questions
            foreach (var questionVm in model.Questions.Where(q => !q.IsDeleted))
            {
                if (questionVm.Id.HasValue)
                {
                    var question = chapter.Questions.FirstOrDefault(q => q.Id == questionVm.Id);
                    if (question != null)
                    {
                        question.Type = questionVm.Type;
                        question.QuestionText = questionVm.QuestionText;
                        question.Answer = questionVm.Answer;
                        question.OptionA = questionVm.OptionA;
                        question.OptionB = questionVm.OptionB;
                        question.OptionC = questionVm.OptionC;
                        question.OptionD = questionVm.OptionD;
                        question.CorrectOption = questionVm.CorrectOption;
                        question.DisplayOrder = questionVm.DisplayOrder;
                    }
                }
                else
                {
                    var newQuestion = new Question
                    {
                        ChapterId = chapter.Id,
                        ChapterSectionId = null, // No longer using sections
                        Type = questionVm.Type,
                        QuestionText = questionVm.QuestionText,
                        Answer = questionVm.Answer,
                        OptionA = questionVm.OptionA,
                        OptionB = questionVm.OptionB,
                        OptionC = questionVm.OptionC,
                        OptionD = questionVm.OptionD,
                        CorrectOption = questionVm.CorrectOption,
                        DisplayOrder = questionVm.DisplayOrder,
                        CreatedAt = DateTime.Now,
                        IsActive = true
                    };
                    _context.Questions.Add(newQuestion);
                }
            }

            // Handle new file uploads
            if (materialFiles != null && materialFiles.Any())
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "academic-materials");
                Directory.CreateDirectory(uploadsFolder);

                for (int i = 0; i < materialFiles.Count; i++)
                {
                    var file = materialFiles[i];
                    if (file?.Length > 0 && i < model.Materials.Count)
                    {
                        var materialViewModel = model.Materials[i];
                        if (materialViewModel.IsDeleted) continue;

                        var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }

                        var material = new ChapterMaterial
                        {
                            ChapterId = chapter.Id,
                            ChapterSectionId = null, // No longer using sections
                            Type = materialViewModel.Type,
                            Heading = materialViewModel.Heading,
                            Description = materialViewModel.Description,
                            FilePath = Path.Combine("uploads", "academic-materials", uniqueFileName).Replace("\\", "/"),
                            OriginalFileName = file.FileName,
                            DisplayOrder = materialViewModel.DisplayOrder,
                            UploadedAt = DateTime.Now,
                            UploadedBy = User.Identity.Name,
                            IsActive = true
                        };
                        _context.ChapterMaterials.Add(material);
                    }
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Chapter updated successfully!";
            return RedirectToAction(nameof(Chapters), new { id = model.SubjectId, classId = classId });
        }

        // POST: AcademicMaterial/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int subjectId, int classId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isTeacher = User.IsInRole("Teacher");

            var chapter = await _context.Chapters.FindAsync(id);
            if (chapter == null)
                return NotFound();

            // Check authorization
            if (!isAdmin && isTeacher)
            {
                var hasAccess = await _context.TeacherAssignments
                    .AnyAsync(ta => ta.TeacherId == currentUser.EmployeeId && 
                                   ta.SubjectId == subjectId && 
                                   ta.ClassId == classId && 
                                   ta.IsActive);
                if (!hasAccess)
                    return Forbid();
            }

            chapter.IsActive = false;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Chapter deleted successfully!";
            return RedirectToAction(nameof(Chapters), new { id = subjectId, classId = classId });
        }

        // POST: AcademicMaterial/DeleteMaterial/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMaterial(int id)
        {
            var material = await _context.ChapterMaterials
                .Include(m => m.Chapter)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (material == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            // Only allow delete if admin or if teacher owns the chapter
            if (!isAdmin)
            {
                var hasAccess = await _context.TeacherAssignments
                    .AnyAsync(ta => ta.TeacherId == currentUser.EmployeeId && 
                                   ta.SubjectId == material.Chapter.SubjectId && 
                                   ta.IsActive);
                if (!hasAccess)
                    return Forbid();
            }

            // Delete physical file
            var filePath = Path.Combine(_env.WebRootPath, material.FilePath);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            _context.ChapterMaterials.Remove(material);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
        
        // GET: /api/chapters/{id}/sections - API endpoint for getting chapter sections
        [HttpGet("/api/chapters/{id}/sections")]
        public async Task<IActionResult> GetChapterSections(int id)
        {
            var sections = await _context.ChapterSections
                .Where(cs => cs.ChapterId == id && cs.IsActive)
                .OrderBy(cs => cs.DisplayOrder)
                .Select(cs => new { cs.Id, cs.Name })
                .ToListAsync();
            
            return Json(sections);
        }
    }
}
