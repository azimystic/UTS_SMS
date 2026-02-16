using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;

namespace SMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SurveyQuestionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SurveyQuestionController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: SurveyQuestion
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            int? campusId = currentUser?.CampusId;

            var query = _context.SurveyQuestions
                .Include(sq => sq.Campus)
                .Where(sq => sq.IsActive);

            // Filter by campus if user is not admin
            if (campusId.HasValue)
            {
                query = query.Where(sq => sq.CampusId == campusId.Value);
            }

            var surveyQuestions = await query
                .OrderBy(sq => sq.QuestionOrder)
                .ThenBy(sq => sq.CreatedDate)
                .ToListAsync();

            // Campus list for admin
            if (!campusId.HasValue)
            {
                ViewBag.CampusList = await _context.Campuses
                    .Where(c => c.IsActive)
                    .ToListAsync();
            }

            return View(surveyQuestions);
        }

        // GET: SurveyQuestion/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var surveyQuestion = await _context.SurveyQuestions
                .Include(sq => sq.Campus)
                .Include(sq => sq.StudentResponses)
                .FirstOrDefaultAsync(sq => sq.Id == id);

            if (surveyQuestion == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.CampusId.HasValue == true && surveyQuestion.CampusId != currentUser.CampusId.Value)
                return Forbid();

            // Get response statistics
            var totalResponses = surveyQuestion.StudentResponses.Count;
            var yesResponses = surveyQuestion.StudentResponses.Count(r => r.Response);
            var noResponses = totalResponses - yesResponses;

            ViewBag.TotalResponses = totalResponses;
            ViewBag.YesResponses = yesResponses;
            ViewBag.NoResponses = noResponses;
            ViewBag.YesPercentage = totalResponses > 0 ? (yesResponses * 100.0 / totalResponses) : 0;
            ViewBag.NoPercentage = totalResponses > 0 ? (noResponses * 100.0 / totalResponses) : 0;

            return View(surveyQuestion);
        }

        // GET: SurveyQuestion/Create
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            await PopulateDropDowns(currentUser?.CampusId);

            var model = new SurveyQuestion
            {
                CampusId = currentUser?.CampusId ?? 0,
                QuestionOrder = await GetNextQuestionOrder(currentUser?.CampusId)
            };

            return View(model);
        }

        // POST: SurveyQuestion/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("QuestionText,QuestionOrder,CampusId")] SurveyQuestion surveyQuestion)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser?.CampusId.HasValue == true && surveyQuestion.CampusId != currentUser.CampusId.Value)
                return Forbid();
            ModelState.Remove("Campus");
            if (ModelState.IsValid)
            {
                surveyQuestion.CreatedBy = currentUser?.FullName;
                surveyQuestion.CreatedDate = DateTime.Now;

                _context.Add(surveyQuestion);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropDowns(currentUser?.CampusId);
            return View(surveyQuestion);
        }

        // GET: SurveyQuestion/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var surveyQuestion = await _context.SurveyQuestions.FindAsync(id);
            if (surveyQuestion == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.CampusId.HasValue == true && surveyQuestion.CampusId != currentUser.CampusId.Value)
                return Forbid();

            await PopulateDropDowns(currentUser?.CampusId, surveyQuestion);
            return View(surveyQuestion);
        }

        // POST: SurveyQuestion/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,QuestionText,QuestionOrder,CampusId,CreatedDate,CreatedBy")] SurveyQuestion surveyQuestion)
        {
            if (id != surveyQuestion.Id)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.CampusId.HasValue == true && surveyQuestion.CampusId != currentUser.CampusId.Value)
                return Forbid();
            ModelState.Remove("Campus");

            if (ModelState.IsValid)
            {
                try
                {
                    surveyQuestion.ModifiedBy = currentUser?.FullName;
                    surveyQuestion.ModifiedDate = DateTime.Now;

                    _context.Update(surveyQuestion);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SurveyQuestionExists(surveyQuestion.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropDowns(currentUser?.CampusId, surveyQuestion);
            return View(surveyQuestion);
        }

        // GET: SurveyQuestion/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var surveyQuestion = await _context.SurveyQuestions
                .Include(sq => sq.Campus)
                .Include(sq => sq.StudentResponses)
                .FirstOrDefaultAsync(sq => sq.Id == id);

            if (surveyQuestion == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.CampusId.HasValue == true && surveyQuestion.CampusId != currentUser.CampusId.Value)
                return Forbid();

            return View(surveyQuestion);
        }

        // POST: SurveyQuestion/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var surveyQuestion = await _context.SurveyQuestions.FindAsync(id);
            if (surveyQuestion != null)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser?.CampusId.HasValue == true && surveyQuestion.CampusId != currentUser.CampusId.Value)
                    return Forbid();

                surveyQuestion.IsActive = false;
                surveyQuestion.ModifiedBy = currentUser?.FullName;
                surveyQuestion.ModifiedDate = DateTime.Now;

                _context.Update(surveyQuestion);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: SurveyQuestion/Reorder
        [HttpPost]
        public async Task<IActionResult> Reorder([FromBody] List<int> questionIds)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            try
            {
                for (int i = 0; i < questionIds.Count; i++)
                {
                    var question = await _context.SurveyQuestions.FindAsync(questionIds[i]);
                    if (question != null)
                    {
                        // Check campus permissions
                        if (currentUser?.CampusId.HasValue == true && question.CampusId != currentUser.CampusId.Value)
                            continue;

                        question.QuestionOrder = i + 1;
                        question.ModifiedBy = currentUser?.FullName;
                        question.ModifiedDate = DateTime.Now;
                        
                        _context.Update(question);
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        private bool SurveyQuestionExists(int id)
        {
            return _context.SurveyQuestions.Any(e => e.Id == id);
        }

        private async Task PopulateDropDowns(int? campusId, SurveyQuestion? surveyQuestion = null)
        {
            // Campus dropdown for admin users
            if (!campusId.HasValue)
            {
                ViewData["CampusId"] = new SelectList(
                    await _context.Campuses.Where(c => c.IsActive).ToListAsync(),
                    "Id",
                    "Name",
                    surveyQuestion?.CampusId
                );
            }
        }

        private async Task<int> GetNextQuestionOrder(int? campusId)
        {
            var query = _context.SurveyQuestions.Where(sq => sq.IsActive);
            
            if (campusId.HasValue)
            {
                query = query.Where(sq => sq.CampusId == campusId.Value);
            }

            var maxOrder = await query.MaxAsync(sq => (int?)sq.QuestionOrder) ?? 0;
            return maxOrder + 1;
        }
    }
}