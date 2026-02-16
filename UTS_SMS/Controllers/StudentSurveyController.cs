using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;

namespace SMS.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentSurveyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentSurveyController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: StudentSurvey
        public async Task<IActionResult> Index(int? subjectId, int? teacherId)
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

            // 1. Get survey questions
            var surveyQuestions = await _context.SurveyQuestions
                .Where(sq => sq.IsActive && sq.CampusId == student.CampusId)
                .OrderBy(sq => sq.QuestionOrder)
                .ToListAsync();

            // 2. Get teacher assignments
            var teacherAssignments = await _context.TeacherAssignments
                .Where(ta => ta.ClassId == student.Class &&
                             ta.SectionId == student.Section &&
                             ta.IsActive)
                .Include(ta => ta.Teacher)
                .Include(ta => ta.Subject)
                .ToListAsync();

            // --- FIX STARTS HERE ---

            // 3. OPTIMIZATION: Fetch relevant existing responses in ONE Database Query
            // We get a list of Subject/Teacher pairs that this student has already answered.
            // We filter by 'activeQuestionIds' to ensure they answered the current version of the survey.
            // Since 'StudentSurveyResponse' does not have SubjectId, we only check TeacherId.
            var activeQuestionIds = surveyQuestions.Select(q => q.Id).ToList();

            var completedTeacherIds = await _context.StudentSurveyResponses
                .Where(r => r.StudentId == student.Id && activeQuestionIds.Contains(r.SurveyQuestionId))
                .Select(r => r.TeacherId) // ? REMOVED SubjectId here
                .Distinct()
                .ToListAsync();

            // 4. Map in Memory
            var subjectTeacherCards = teacherAssignments
                .GroupBy(ta => new { ta.SubjectId, ta.TeacherId, SubjectName = ta.Subject.Name, TeacherName = ta.Teacher.FullName })
                .Select(g => new
                {
                    SubjectId = g.Key.SubjectId,
                    SubjectName = g.Key.SubjectName,
                    TeacherId = g.Key.TeacherId,
                    TeacherName = g.Key.TeacherName,

                    // ? UPDATED CHECK: simply check if this TeacherId exists in our list
                    IsFilled = completedTeacherIds.Contains(g.Key.TeacherId)
                })
                .ToList();
            // --- FIX ENDS HERE ---

            // Because the object above is Anonymous, we cast it to dynamic for ViewBag to work in Razor
            ViewBag.SubjectTeacherCards = subjectTeacherCards.Select(x => (dynamic)x).ToList();

            ViewBag.Student = student;
            ViewBag.SurveyQuestions = surveyQuestions;
            ViewBag.SelectedSubjectId = subjectId;
            ViewBag.SelectedTeacherId = teacherId;

            // Load form if selected
            if (subjectId.HasValue && teacherId.HasValue)
            {
                var existingResponses = await _context.StudentSurveyResponses
                    .Where(ssr => ssr.StudentId == student.Id && ssr.TeacherId == teacherId)
                    .ToListAsync();

                ViewBag.ExistingResponses = existingResponses.ToDictionary(r => r.SurveyQuestionId, r => r.Response);

                // Logic to determine if specifically THIS survey is complete based on current questions
                ViewBag.HasCompletedSurvey = surveyQuestions.Count > 0 &&
                    surveyQuestions.All(q => existingResponses.Any(r => r.SurveyQuestionId == q.Id));
            }

            return View();
        }

        // POST: StudentSurvey/Submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(Dictionary<int, bool> responses, int? subjectId, int? teacherId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.StudentId == null)
            {
                return BadRequest("Invalid student session");
            }

            if (!teacherId.HasValue)
            {
                TempData["ErrorMessage"] = "Invalid teacher selection";
                return RedirectToAction(nameof(Index));
            }

            var student = await _context.Students
                .Include(s => s.Campus)
                .FirstOrDefaultAsync(s => s.Id == currentUser.StudentId);

            if (student == null)
            {
                return BadRequest("Student not found");
            }

            try
            {
                // Get all active survey questions for this campus
                var surveyQuestions = await _context.SurveyQuestions
                    .Where(sq => sq.IsActive && sq.CampusId == student.CampusId)
                    .ToListAsync();

                // Remove existing responses for this student and teacher
                var existingResponses = await _context.StudentSurveyResponses
                    .Where(ssr => ssr.StudentId == student.Id && ssr.TeacherId == teacherId.Value)
                    .ToListAsync();

                _context.StudentSurveyResponses.RemoveRange(existingResponses);

                // Add new responses
                foreach (var response in responses)
                {
                    var questionId = response.Key;
                    var answer = response.Value;

                    // Verify this is a valid question for this campus
                    if (surveyQuestions.Any(sq => sq.Id == questionId))
                    {
                        var surveyResponse = new StudentSurveyResponse
                        {
                            StudentId = student.Id,
                            SurveyQuestionId = questionId,
                            Response = answer,
                            ResponseDate = DateTime.Now,
                            CampusId = student.CampusId,
                            TeacherId = teacherId.Value
                        };

                        _context.StudentSurveyResponses.Add(surveyResponse);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Survey submitted successfully! Thank you for your feedback.";
                return RedirectToAction(nameof(Index), new { subjectId = subjectId, teacherId = teacherId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while saving your responses. Please try again.";
                return RedirectToAction(nameof(Index), new { subjectId = subjectId, teacherId = teacherId });
            }
        }

    }
}