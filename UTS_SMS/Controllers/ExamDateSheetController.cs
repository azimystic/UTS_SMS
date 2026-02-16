using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using System.Text.Json;

namespace SMS.Controllers
{
    [Authorize]
    public class ExamDateSheetController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExamDateSheetController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: ExamDateSheet - Calendar View
        public async Task<IActionResult> Index(int? campusId, DateTime? month)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            var isOwner = User.IsInRole("Owner");
            
            var selectedMonth = month ?? DateTime.Now;
            var startOfMonth = new DateTime(selectedMonth.Year, selectedMonth.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var viewModel = new ExamDateSheetCalendarViewModel
            {
                SelectedMonth = selectedMonth,
                IsUserCampusLocked = userCampusId.HasValue && !isOwner,
                SelectedCampusId = userCampusId.HasValue && !isOwner ? userCampusId : campusId
            };

            // Get campuses for dropdown
            if (isOwner)
            {
                viewModel.Campuses = await _context.Campuses.Where(c => c.IsActive).ToListAsync();
            }
            else if (userCampusId.HasValue)
            {
                viewModel.Campuses = await _context.Campuses.Where(c => c.Id == userCampusId && c.IsActive).ToListAsync();
            }

            // Build query for exam date sheets
            var query = _context.ExamDateSheets
                .Include(e => e.ExamCategory)
                .Include(e => e.Exam)
                .Include(e => e.Subject)
                .Include(e => e.Campus)
                .Include(e => e.ClassSections)
                    .ThenInclude(cs => cs.Class)
                .Include(e => e.ClassSections)
                    .ThenInclude(cs => cs.Section)
                .Where(e => e.IsActive && e.ExamDate >= startOfMonth && e.ExamDate <= endOfMonth);

            // Filter by campus
            if (viewModel.SelectedCampusId.HasValue)
            {
                query = query.Where(e => e.CampusId == viewModel.SelectedCampusId.Value);
            }

            viewModel.ExamDateSheets = await query.OrderBy(e => e.ExamDate).ToListAsync();

            return View(viewModel);
        }

        // GET: ExamDateSheet/Create
        public async Task<IActionResult> Create(string date)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            var isOwner = User.IsInRole("Owner");

            DateTime selectedDate = DateTime.Now;
            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out DateTime parsedDate))
            {
                selectedDate = parsedDate;
            }

            var viewModel = new ExamDateSheetViewModel
            {
                ExamDate = selectedDate,
                IsUserCampusLocked = userCampusId.HasValue && !isOwner,
                CampusId = userCampusId
            };

            await PopulateDropdowns(viewModel, userCampusId, isOwner);

            return View(viewModel);
        }

        // POST: ExamDateSheet/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExamDateSheetViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            var isOwner = User.IsInRole("Owner");

            // If user is not owner and has a campus, force that campus
            if (!isOwner && userCampusId.HasValue)
            {
                model.CampusId = userCampusId;
            }

            ModelState.Remove("ExamCategories");
            ModelState.Remove("Exams");
            ModelState.Remove("Subjects");
            ModelState.Remove("Campuses");
            ModelState.Remove("Classes");
            ModelState.Remove("Sections");
            ModelState.Remove("ClassSectionRows");

            if (ModelState.IsValid && model.CampusId.HasValue && model.ExamCategoryId.HasValue 
                && model.ExamId.HasValue && model.SubjectId.HasValue)
            {
                // VALIDATION: Check if selected classes have academic years assigned
                if (model.ClassSectionRows != null && model.ClassSectionRows.Any())
                {
                    var classIds = model.ClassSectionRows.Select(r => r.ClassId).Distinct().ToList();
                    var classesWithoutYear = await _context.Classes
                        .Where(c => classIds.Contains(c.Id) && string.IsNullOrEmpty(c.CurrentAcademicYear))
                        .Select(c => c.Name)
                        .ToListAsync();
                    
                    if (classesWithoutYear.Any())
                    {
                        ModelState.AddModelError("", 
                            $"⚠️ The following class(es) do not have a Current Academic Year assigned: {string.Join(", ", classesWithoutYear)}. " +
                            $"Please assign an academic year before creating an exam.");
                        await PopulateDropdowns(model, userCampusId, isOwner);
                        return View(model);
                    }
                    
                    // Get the academic year from the first selected class (all should have the same year)
                    var firstClass = await _context.Classes.FindAsync(model.ClassSectionRows.First().ClassId);
                    if (firstClass != null && !string.IsNullOrEmpty(firstClass.CurrentAcademicYear))
                    {
                        // Parse academic year (e.g., "2025-2026" -> 2025)
                        var yearParts = firstClass.CurrentAcademicYear.Split('-');
                        if (yearParts.Length > 0 && int.TryParse(yearParts[0], out int parsedYear))
                        {
                            model.AcademicYear = parsedYear;
                        }
                    }
                }

                // Validation: Check if the same exam, class, section, subject, and academic year is already scheduled
                if (model.ClassSectionRows != null && model.ClassSectionRows.Any())
                {
                    foreach (var row in model.ClassSectionRows.Where(r => r.ClassId > 0 && r.SectionId > 0))
                    {
                        var existingExam = await _context.ExamDateSheets
                            .Include(e => e.ClassSections)
                            .Where(e => e.IsActive 
                                && e.ExamId == model.ExamId.Value 
                                && e.SubjectId == model.SubjectId.Value 
                                && e.CampusId == model.CampusId.Value
                                && e.AcademicYear == model.AcademicYear
                                && e.ExamDate.Date != model.ExamDate.Date)
                            .FirstOrDefaultAsync(e => e.ClassSections.Any(cs => 
                                cs.IsActive && cs.ClassId == row.ClassId && cs.SectionId == row.SectionId));

                        if (existingExam != null)
                        {
                            var className = (await _context.Classes.FindAsync(row.ClassId))?.Name ?? "Unknown";
                            var sectionName = (await _context.ClassSections.FindAsync(row.SectionId))?.Name ?? "Unknown";
                            var subjectName = (await _context.Subjects.FindAsync(model.SubjectId.Value))?.Name ?? "Unknown";
                            var examName = (await _context.Exams.FindAsync(model.ExamId.Value))?.Name ?? "Unknown";
                            
                            ModelState.AddModelError("", 
                                $"An exam for {examName} - {subjectName} for class {className} section {sectionName} is already scheduled on {existingExam.ExamDate.ToString("yyyy-MM-dd")} for academic year {model.AcademicYear}. " +
                                $"Cannot schedule the same exam on multiple dates.");
                            
                            await PopulateDropdowns(model, userCampusId, isOwner);
                            return View(model);
                        }
                    }
                }

                var examDateSheet = new ExamDateSheet
                {
                    ExamDate = model.ExamDate,
                    ExamCategoryId = model.ExamCategoryId.Value,
                    ExamId = model.ExamId.Value,
                    SubjectId = model.SubjectId.Value,
                    CampusId = model.CampusId.Value,
                    TotalMarks = model.TotalMarks,
                    PassingMarks = model.PassingMarks,
                    AcademicYear = model.AcademicYear,
                    CreatedBy = User.Identity?.Name ?? "System",
                    IsActive = true
                };

                _context.ExamDateSheets.Add(examDateSheet);
                await _context.SaveChangesAsync();

                // Add class-section mappings
                if (model.ClassSectionRows != null && model.ClassSectionRows.Any())
                {
                    foreach (var row in model.ClassSectionRows.Where(r => r.ClassId > 0 && r.SectionId > 0))
                    {
                        var classSection = new ExamDateSheetClassSection
                        {
                            ExamDateSheetId = examDateSheet.Id,
                            ClassId = row.ClassId,
                            SectionId = row.SectionId,
                            IsActive = true
                        };
                        _context.ExamDateSheetClassSections.Add(classSection);
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Exam date sheet created successfully!";
                return RedirectToAction(nameof(Index), new { month = model.ExamDate.ToString("yyyy-MM-01") });
            }

            await PopulateDropdowns(model, userCampusId, isOwner);
            return View(model);
        }

        // GET: ExamDateSheet/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            var isOwner = User.IsInRole("Owner");

            var examDateSheet = await _context.ExamDateSheets
                .Include(e => e.ClassSections)
                .FirstOrDefaultAsync(e => e.Id == id && e.IsActive);

            if (examDateSheet == null)
            {
                return NotFound();
            }

            // Check if user has access to this exam date sheet
            if (!isOwner && userCampusId.HasValue && examDateSheet.CampusId != userCampusId)
            {
                return Forbid();
            }

            var viewModel = new ExamDateSheetViewModel
            {
                Id = examDateSheet.Id,
                ExamDate = examDateSheet.ExamDate,
                ExamCategoryId = examDateSheet.ExamCategoryId,
                ExamId = examDateSheet.ExamId,
                SubjectId = examDateSheet.SubjectId,
                CampusId = examDateSheet.CampusId,
                TotalMarks = examDateSheet.TotalMarks,
                PassingMarks = examDateSheet.PassingMarks,
                AcademicYear = examDateSheet.AcademicYear,
                IsUserCampusLocked = userCampusId.HasValue && !isOwner,
                ClassSectionRows = examDateSheet.ClassSections
                    .Where(cs => cs.IsActive)
                    .Select(cs => new ClassSectionRow { ClassId = cs.ClassId, SectionId = cs.SectionId })
                    .ToList()
            };

            await PopulateDropdowns(viewModel, userCampusId, isOwner);

            return View(viewModel);
        }

        // POST: ExamDateSheet/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ExamDateSheetViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            var isOwner = User.IsInRole("Owner");

            // If user is not owner and has a campus, force that campus
            if (!isOwner && userCampusId.HasValue)
            {
                model.CampusId = userCampusId;
            }

            ModelState.Remove("ExamCategories");
            ModelState.Remove("Exams");
            ModelState.Remove("Subjects");
            ModelState.Remove("Campuses");
            ModelState.Remove("Classes");
            ModelState.Remove("Sections");
            ModelState.Remove("ClassSectionRows");

            if (ModelState.IsValid && model.CampusId.HasValue && model.ExamCategoryId.HasValue 
                && model.ExamId.HasValue && model.SubjectId.HasValue)
            {
                var examDateSheet = await _context.ExamDateSheets
                    .Include(e => e.ClassSections)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (examDateSheet == null)
                {
                    return NotFound();
                }

                // Check if user has access
                if (!isOwner && userCampusId.HasValue && examDateSheet.CampusId != userCampusId)
                {
                    return Forbid();
                }

                // Validation: Check if the same exam, class, section, and subject is already scheduled on a different date
                if (model.ClassSectionRows != null && model.ClassSectionRows.Any())
                {
                    foreach (var row in model.ClassSectionRows.Where(r => r.ClassId > 0 && r.SectionId > 0))
                    {
                        var existingExam = await _context.ExamDateSheets
                            .Include(e => e.ClassSections)
                            .Where(e => e.IsActive 
                                && e.Id != id  // Exclude current record
                                && e.ExamId == model.ExamId.Value 
                                && e.SubjectId == model.SubjectId.Value 
                                && e.CampusId == model.CampusId.Value
                                && e.ExamDate.Date != model.ExamDate.Date)
                            .FirstOrDefaultAsync(e => e.ClassSections.Any(cs => 
                                cs.IsActive && cs.ClassId == row.ClassId && cs.SectionId == row.SectionId));

                        if (existingExam != null)
                        {
                            var className = (await _context.Classes.FindAsync(row.ClassId))?.Name ?? "Unknown";
                            var sectionName = (await _context.ClassSections.FindAsync(row.SectionId))?.Name ?? "Unknown";
                            var subjectName = (await _context.Subjects.FindAsync(model.SubjectId.Value))?.Name ?? "Unknown";
                            var examName = (await _context.Exams.FindAsync(model.ExamId.Value))?.Name ?? "Unknown";
                            
                            ModelState.AddModelError("", 
                                $"An exam for {examName} - {subjectName} for class {className} section {sectionName} is already scheduled on {existingExam.ExamDate.ToString("yyyy-MM-dd")}. " +
                                $"Cannot schedule the same exam on multiple dates.");
                            
                            await PopulateDropdowns(model, userCampusId, isOwner);
                            return View(model);
                        }
                    }
                }

                examDateSheet.ExamDate = model.ExamDate;
                examDateSheet.ExamCategoryId = model.ExamCategoryId.Value;
                examDateSheet.ExamId = model.ExamId.Value;
                examDateSheet.SubjectId = model.SubjectId.Value;
                examDateSheet.CampusId = model.CampusId.Value;
                examDateSheet.TotalMarks = model.TotalMarks;
                examDateSheet.PassingMarks = model.PassingMarks;
                examDateSheet.ModifiedBy = User.Identity?.Name ?? "System";
                examDateSheet.ModifiedDate = DateTime.Now;

                // Remove existing class-section mappings
                _context.ExamDateSheetClassSections.RemoveRange(examDateSheet.ClassSections);

                // Add new class-section mappings
                if (model.ClassSectionRows != null && model.ClassSectionRows.Any())
                {
                    foreach (var row in model.ClassSectionRows.Where(r => r.ClassId > 0 && r.SectionId > 0))
                    {
                        var classSection = new ExamDateSheetClassSection
                        {
                            ExamDateSheetId = examDateSheet.Id,
                            ClassId = row.ClassId,
                            SectionId = row.SectionId,
                            IsActive = true
                        };
                        _context.ExamDateSheetClassSections.Add(classSection);
                    }
                }

                // ✅ Check if marks changed and trigger recalculation
                var marksChanged = examDateSheet.TotalMarks != model.TotalMarks || 
                                   examDateSheet.PassingMarks != model.PassingMarks;

                await _context.SaveChangesAsync();

                if (marksChanged)
                {
                    // Trigger recalculation of all affected exam marks
                    var examMarksToUpdate = await _context.ExamMarks
                        .Where(em => em.ExamId == examDateSheet.ExamId && 
                                    em.SubjectId == examDateSheet.SubjectId &&
                                    em.CampusId == examDateSheet.CampusId)
                        .ToListAsync();
                    
                    foreach (var mark in examMarksToUpdate)
                    {
                        mark.TotalMarks = examDateSheet.TotalMarks;
                        mark.PassingMarks = examDateSheet.PassingMarks;
                        mark.CalculateStatusAndGrade();
                    }
                    
                    _context.ExamMarks.UpdateRange(examMarksToUpdate);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Exam date sheet updated successfully!";
                return RedirectToAction(nameof(Index), new { month = model.ExamDate.ToString("yyyy-MM-01") });
            }

            await PopulateDropdowns(model, userCampusId, isOwner);
            return View(model);
        }

        // POST: ExamDateSheet/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            var isOwner = User.IsInRole("Owner");

            var examDateSheet = await _context.ExamDateSheets.FindAsync(id);
            if (examDateSheet == null)
            {
                return NotFound();
            }

            // Check if user has access
            if (!isOwner && userCampusId.HasValue && examDateSheet.CampusId != userCampusId)
            {
                return Forbid();
            }

            examDateSheet.IsActive = false;
            examDateSheet.ModifiedBy = User.Identity?.Name ?? "System";
            examDateSheet.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Exam date sheet deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // API: Get Exams by Category
        [HttpGet]
        public async Task<JsonResult> GetExamsByCategory(int categoryId)
        {
            var exams = await _context.Exams
                .Where(e => e.ExamCategoryId == categoryId && e.IsActive)
                .Select(e => new { id = e.Id, name = e.Name })
                .ToListAsync();

            return Json(exams);
        }

        // API: Get Exam Categories by Campus (including "All Campuses" categories)
        [HttpGet]
        public async Task<JsonResult> GetExamCategoriesByCampus(int campusId)
        {
            var categories = await _context.ExamCategories
                .Where(ec => ec.IsActive && (ec.CampusId == campusId || ec.CampusId == null))
                .Select(ec => new { id = ec.Id, name = ec.Name, isAllCampuses = ec.CampusId == null })
                .ToListAsync();

            return Json(categories);
        }

        // API: Get Classes by Campus
        [HttpGet]
        public async Task<JsonResult> GetClassesByCampus(int campusId)
        {
            var classes = await _context.Classes
                .Where(c => c.CampusId == campusId && c.IsActive)
                .Select(c => new { id = c.Id, name = c.Name })
                .ToListAsync();

            return Json(classes);
        }

        // API: Get Sections by Class
        [HttpGet]
        public async Task<JsonResult> GetSectionsByClass(int classId)
        {
            var sections = await _context.ClassSections
                .Where(cs => cs.ClassId == classId && cs.IsActive)
                .Select(cs => new { id = cs.Id, name = cs.Name })
                .ToListAsync();

            return Json(sections);
        }

        // API: Get Subjects by Campus
        [HttpGet]
        public async Task<JsonResult> GetSubjectsByCampus(int campusId)
        {
            var subjects = await _context.Subjects
                .Where(s => s.CampusId == campusId && s.IsActive)
                .Select(s => new { id = s.Id, name = s.Name })
                .ToListAsync();

            return Json(subjects);
        }

        // API: Get Exam Date Sheets for a specific date
        [HttpGet]
        public async Task<JsonResult> GetExamsByDate(DateTime date, int? campusId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;
            var isOwner = User.IsInRole("Owner");

            var query = _context.ExamDateSheets
                .Include(e => e.ExamCategory)
                .Include(e => e.Exam)
                .Include(e => e.Subject)
                .Include(e => e.Campus)
                .Include(e => e.ClassSections)
                    .ThenInclude(cs => cs.Class)
                .Include(e => e.ClassSections)
                    .ThenInclude(cs => cs.Section)
                .Where(e => e.IsActive && e.ExamDate.Date == date.Date);

            // Filter by campus
            if (!isOwner && userCampusId.HasValue)
            {
                query = query.Where(e => e.CampusId == userCampusId.Value);
            }
            else if (campusId.HasValue)
            {
                query = query.Where(e => e.CampusId == campusId.Value);
            }

            var exams = await query.Select(e => new
            {
                id = e.Id,
                examCategory = e.ExamCategory != null ? e.ExamCategory.Name : "",
                examName = e.Exam != null ? e.Exam.Name : "",
                subject = e.Subject != null ? e.Subject.Name : "",
                campus = e.Campus != null ? e.Campus.Name : "",
                totalMarks = e.TotalMarks,
                passingMarks = e.PassingMarks,
                classSections = e.ClassSections.Where(cs => cs.IsActive).Select(cs => new
                {
                    className = cs.Class != null ? cs.Class.Name : "",
                    sectionName = cs.Section != null ? cs.Section.Name : ""
                })
            }).ToListAsync();

            return Json(exams);
        }

        private async Task PopulateDropdowns(ExamDateSheetViewModel viewModel, int? userCampusId, bool isOwner)
        {
            // Campuses
            if (isOwner)
            {
                viewModel.Campuses = await _context.Campuses.Where(c => c.IsActive).ToListAsync();
            }
            else if (userCampusId.HasValue)
            {
                viewModel.Campuses = await _context.Campuses.Where(c => c.Id == userCampusId && c.IsActive).ToListAsync();
            }

            // Exam Categories (including "All Campuses" ones)
            if (viewModel.CampusId.HasValue)
            {
                viewModel.ExamCategories = await _context.ExamCategories
                    .Where(ec => ec.IsActive && (ec.CampusId == viewModel.CampusId || ec.CampusId == null))
                    .ToListAsync();
            }
            else
            {
                viewModel.ExamCategories = await _context.ExamCategories.Where(ec => ec.IsActive).ToListAsync();
            }

            // Exams based on selected category
            if (viewModel.ExamCategoryId.HasValue)
            {
                viewModel.Exams = await _context.Exams
                    .Where(e => e.ExamCategoryId == viewModel.ExamCategoryId && e.IsActive)
                    .ToListAsync();
            }

            // Subjects based on campus
            if (viewModel.CampusId.HasValue)
            {
                viewModel.Subjects = await _context.Subjects
                    .Where(s => s.CampusId == viewModel.CampusId && s.IsActive)
                    .ToListAsync();
            }
            else
            {
                viewModel.Subjects = await _context.Subjects.Where(s => s.IsActive).ToListAsync();
            }

            // Classes based on campus
            if (viewModel.CampusId.HasValue)
            {
                viewModel.Classes = await _context.Classes
                    .Where(c => c.CampusId == viewModel.CampusId && c.IsActive)
                    .ToListAsync();
            }

            // Sections - load all for now, will be filtered by JavaScript
            viewModel.Sections = await _context.ClassSections.Where(cs => cs.IsActive).ToListAsync();
        }
    }
}
