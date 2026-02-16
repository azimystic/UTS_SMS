using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMS;
using SMS.Models;
using SMS.ViewModels;

namespace SMS.Controllers
{
    [Authorize(Roles = "Student")]
    public class AcademicMaterialStudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AcademicMaterialStudentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: AcademicMaterialStudent/Index - List student's subjects
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            if (!currentUser.StudentId.HasValue)
                return NotFound("Student record not found.");
                
            var student = await _context.Students
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .FirstOrDefaultAsync(s => s.Id == currentUser.StudentId.Value);

            if (student == null)
                return NotFound("Student record not found.");

            // Get subjects from SubjectsGrouping
            var subjects = await _context.SubjectsGroupingDetails
                .Where(sgd => sgd.SubjectsGrouping.Id == student.SubjectsGroupingId && sgd.IsActive)
                .Include(sgd => sgd.Subject)
                .Select(sgd => sgd.Subject)
                .Distinct()
                .ToListAsync();

            ViewBag.Student = student;
            return View(subjects);
        }

        // GET: AcademicMaterialStudent/Chapters/5 - List chapters for a subject
        public async Task<IActionResult> Chapters(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            if (!currentUser.StudentId.HasValue)
                return NotFound("Student record not found.");
                
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == currentUser.StudentId.Value);

            if (student == null)
                return NotFound("Student record not found.");

            // Verify student has access to this subject
            var hasAccess = await _context.SubjectsGroupingDetails
                .AnyAsync(sgd => sgd.SubjectsGroupingId == student.SubjectsGroupingId && 
                               sgd.SubjectId == id && 
                               sgd.IsActive);

            if (!hasAccess)
                return Forbid();

            var subject = await _context.Subjects.FindAsync(id);
            var chapters = await _context.Chapters
                .Where(c => c.SubjectId == id && c.IsActive)
                .OrderBy(c => c.ChapterNumber)
                .ToListAsync();

            ViewBag.Subject = subject;
            return View(chapters);
        }

        // GET: AcademicMaterialStudent/ViewChapter/5 - View chapter details
        public async Task<IActionResult> ViewChapter(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            if (!currentUser.StudentId.HasValue)
                return NotFound("Student record not found.");
                
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == currentUser.StudentId.Value);

            if (student == null)
                return NotFound("Student record not found.");

            var chapter = await _context.Chapters
                .Include(c => c.Subject)
                .Include(c => c.ChapterSections)
                .Include(c => c.Questions)
                .Include(c => c.ChapterMaterials)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (chapter == null)
                return NotFound();

            // Verify student has access to this subject
            var hasAccess = await _context.SubjectsGroupingDetails
                .AnyAsync(sgd => sgd.SubjectsGroupingId == student.SubjectsGroupingId && 
                               sgd.SubjectId == chapter.SubjectId && 
                               sgd.IsActive);

            if (!hasAccess)
                return Forbid();

            var model = new StudentAcademicMaterialViewModel
            {
                Chapter = chapter,
                Sections = chapter.ChapterSections.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ToList(),
                Questions = chapter.Questions.Where(q => q.IsActive).OrderBy(q => q.DisplayOrder).ToList(),
                Materials = chapter.ChapterMaterials.Where(m => m.IsActive).OrderBy(m => m.DisplayOrder).ToList()
            };

            return View(model);
        }

        // GET: AcademicMaterialStudent/TakeMCQQuiz/5 - Take MCQ quiz for a chapter
        public async Task<IActionResult> TakeMCQQuiz(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            if (!currentUser.StudentId.HasValue)
                return NotFound("Student record not found.");
                
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == currentUser.StudentId.Value);

            if (student == null)
                return NotFound("Student record not found.");

            var chapter = await _context.Chapters
                .Include(c => c.Subject)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (chapter == null)
                return NotFound();

            // Verify student has access to this subject
            var hasAccess = await _context.SubjectsGroupingDetails
                .AnyAsync(sgd => sgd.SubjectsGroupingId == student.SubjectsGroupingId && 
                               sgd.SubjectId == chapter.SubjectId && 
                               sgd.IsActive);

            if (!hasAccess)
                return Forbid();

            var mcqs = await _context.Questions
                .Where(q => q.ChapterId == id && q.Type == QuestionType.MCQ && q.IsActive)
                .OrderBy(q => q.DisplayOrder)
                .ToListAsync();

            ViewBag.Chapter = chapter;
            return View(mcqs);
        }

        // POST: AcademicMaterialStudent/SubmitMCQQuiz
        [HttpPost]
        public async Task<IActionResult> SubmitMCQQuiz(int chapterId, Dictionary<int, string> answers)
        {
            var mcqs = await _context.Questions
                .Where(q => q.ChapterId == chapterId && q.Type == QuestionType.MCQ && q.IsActive)
                .ToListAsync();

            int correctCount = 0;
            int totalQuestions = mcqs.Count;

            var results = new List<object>();

            foreach (var mcq in mcqs)
            {
                string studentAnswer = answers.ContainsKey(mcq.Id) ? answers[mcq.Id] : "";
                bool isCorrect = studentAnswer.Equals(mcq.CorrectOption, StringComparison.OrdinalIgnoreCase);
                
                if (isCorrect)
                    correctCount++;

                results.Add(new
                {
                    questionId = mcq.Id,
                    isCorrect = isCorrect,
                    correctAnswer = mcq.CorrectOption,
                    studentAnswer = studentAnswer
                });
            }

            var score = totalQuestions > 0 ? (correctCount * 100.0 / totalQuestions) : 0;

            return Json(new
            {
                success = true,
                score = Math.Round(score, 2),
                correctCount = correctCount,
                totalQuestions = totalQuestions,
                results = results
            });
        }
    }
}
