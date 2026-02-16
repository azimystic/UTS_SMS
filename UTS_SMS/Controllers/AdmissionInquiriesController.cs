using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using Microsoft.AspNetCore.Identity;

namespace UTS_SMS.Controllers
{
    public class AdmissionInquiriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdmissionInquiriesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: AdmissionInquiries
        public async Task<IActionResult> Index(string searchString, string statusFilter, int? classFilter)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            ViewData["CurrentFilter"] = searchString;
            ViewData["StatusFilter"] = statusFilter;
            ViewData["ClassFilter"] = classFilter;

            // Status options for dropdown
            ViewBag.StatusOptions = new List<string> { "New", "Contacted", "Visited", "Enrolled", "Rejected" };

            // Class options for dropdown
            var classQuery = _context.Classes.Where(c => c.IsActive);
            if (campusId.HasValue && campusId > 0)
            {
                classQuery = classQuery.Where(c => c.CampusId == campusId);
            }
            ViewBag.ClassOptions = await classQuery.ToListAsync();

            var inquiriesQuery = _context.AdmissionInquiries
                .Include(a => a.ClassInterested)
                .Include(a => a.Campus)
                .Where(a => a.IsActive);

            // Apply campus filter
            if (campusId.HasValue && campusId > 0)
            {
                inquiriesQuery = inquiriesQuery.Where(a => a.CampusId == campusId);
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                inquiriesQuery = inquiriesQuery.Where(a => 
                    a.StudentName.Contains(searchString) ||
                    a.FatherName.Contains(searchString) ||
                    a.PhoneNumber.Contains(searchString) ||
                    a.Email.Contains(searchString));
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(statusFilter))
            {
                inquiriesQuery = inquiriesQuery.Where(a => a.InquiryStatus == statusFilter);
            }

            // Apply class filter
            if (classFilter.HasValue)
            {
                inquiriesQuery = inquiriesQuery.Where(a => a.ClassInterestedId == classFilter);
            }

            var inquiries = await inquiriesQuery
                .OrderByDescending(a => a.InquiryDate)
                .ToListAsync();

            return View(inquiries);
        }

        // GET: AdmissionInquiries/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var admissionInquiry = await _context.AdmissionInquiries
                .Include(a => a.ClassInterested)
                .Include(a => a.Campus)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (admissionInquiry == null)
            {
                return NotFound();
            }

            return View(admissionInquiry);
        }

        // GET: AdmissionInquiries/Create
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            var classQuery = _context.Classes.Where(c => c.IsActive);
            if (campusId.HasValue && campusId > 0)
            {
                classQuery = classQuery.Where(c => c.CampusId == campusId);
                ViewData["CampusId"] = new SelectList(
                    _context.Campuses.Where(c => c.IsActive && c.Id == campusId),
                    "Id", "Name", campusId);
            }
            else
            {
                ViewData["CampusId"] = new SelectList(
                    _context.Campuses.Where(c => c.IsActive),
                    "Id", "Name");
            }

            ViewData["ClassInterestedId"] = new SelectList(await classQuery.ToListAsync(), "Id", "Name");
            ViewBag.StatusOptions = new List<string> { "New", "Contacted", "Visited", "Enrolled", "Rejected" };
            ViewBag.SourceOptions = new List<string> { "Website", "Referral", "Walk-in", "Advertisement", "Social Media", "Other" };

            return View();
        }

        // POST: AdmissionInquiries/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StudentName,FatherName,PhoneNumber,Email,ClassInterestedId,CampusId,PreviousSchool,InquiryStatus,VisitDate,Remarks,Source,FollowUpDate,FollowUpRequired")] AdmissionInquiry admissionInquiry)
        {
            ModelState.Remove("ClassInterested");
            ModelState.Remove("Campus");
            if (ModelState.IsValid)
            {
                admissionInquiry.InquiryDate = DateTime.Now;
                admissionInquiry.CreatedBy = User.Identity.Name;
                admissionInquiry.IsActive = true;

                _context.Add(admissionInquiry);
                await _context.SaveChangesAsync();

                // Create calendar event and todo if follow-up is required
                if (admissionInquiry.FollowUpRequired && admissionInquiry.FollowUpDate.HasValue)
                {
                    await CreateFollowUpCalendarAndTodo(admissionInquiry);
                }

                TempData["SuccessMessage"] = "Admission inquiry created successfully!";
                return RedirectToAction(nameof(Index));
            }

            // Reload dropdowns if validation fails
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            var classQuery = _context.Classes.Where(c => c.IsActive);
            if (campusId.HasValue && campusId > 0)
            {
                classQuery = classQuery.Where(c => c.CampusId == campusId);
            }

            ViewData["ClassInterestedId"] = new SelectList(await classQuery.ToListAsync(), "Id", "Name", admissionInquiry.ClassInterestedId);
            ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name", admissionInquiry.CampusId);
            ViewBag.StatusOptions = new List<string> { "New", "Contacted", "Visited", "Enrolled", "Rejected" };
            ViewBag.SourceOptions = new List<string> { "Website", "Referral", "Walk-in", "Advertisement", "Social Media", "Other" };

            return View(admissionInquiry);
        }

        // GET: AdmissionInquiries/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var admissionInquiry = await _context.AdmissionInquiries.FindAsync(id);
            if (admissionInquiry == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            var classQuery = _context.Classes.Where(c => c.IsActive);
            if (campusId.HasValue && campusId > 0)
            {
                classQuery = classQuery.Where(c => c.CampusId == campusId);
            }

            ViewData["ClassInterestedId"] = new SelectList(await classQuery.ToListAsync(), "Id", "Name", admissionInquiry.ClassInterestedId);
            ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name", admissionInquiry.CampusId);
            ViewBag.StatusOptions = new List<string> { "New", "Contacted", "Visited", "Enrolled", "Rejected" };
            ViewBag.SourceOptions = new List<string> { "Website", "Referral", "Walk-in", "Advertisement", "Social Media", "Other" };

            return View(admissionInquiry);
        }

        // POST: AdmissionInquiries/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StudentName,FatherName,PhoneNumber,Email,ClassInterestedId,CampusId,PreviousSchool,InquiryStatus,VisitDate,Remarks,Source,InquiryDate,CreatedBy,FollowUpDate,FollowUpRequired,IsActive")] AdmissionInquiry admissionInquiry)
        {
            if (id != admissionInquiry.Id)
            {
                return NotFound();
            }
            ModelState.Remove("ClassInterested");
            ModelState.Remove("Campus");
            if (ModelState.IsValid)
            {
                try
                {
                    // Get the original inquiry to track changes
                    var originalInquiry = await _context.AdmissionInquiries
                        .AsNoTracking()
                        .FirstOrDefaultAsync(i => i.Id == id);

                    if (originalInquiry == null)
                    {
                        return NotFound();
                    }

                    // Track if follow-up was removed or date changed
                    bool followUpRemoved = originalInquiry.FollowUpRequired && !admissionInquiry.FollowUpRequired;
                    bool followUpDateChanged = originalInquiry.FollowUpRequired && admissionInquiry.FollowUpRequired &&
                                              originalInquiry.FollowUpDate != admissionInquiry.FollowUpDate;

                    // Remove old calendar/todo entries if follow-up is removed or date changed
                    if (followUpRemoved || followUpDateChanged)
                    {
                        await RemoveFollowUpCalendarAndTodo(originalInquiry);
                    }

                    _context.Update(admissionInquiry);
                    await _context.SaveChangesAsync();

                    // Create new calendar event and todo if follow-up is required
                    if (admissionInquiry.FollowUpRequired && admissionInquiry.FollowUpDate.HasValue)
                    {
                        await CreateFollowUpCalendarAndTodo(admissionInquiry);
                    }

                    TempData["SuccessMessage"] = "Admission inquiry updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AdmissionInquiryExists(admissionInquiry.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            // Reload dropdowns if validation fails
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            var classQuery = _context.Classes.Where(c => c.IsActive);
            if (campusId.HasValue && campusId > 0)
            {
                classQuery = classQuery.Where(c => c.CampusId == campusId);
            }

            ViewData["ClassInterestedId"] = new SelectList(await classQuery.ToListAsync(), "Id", "Name", admissionInquiry.ClassInterestedId);
            ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name", admissionInquiry.CampusId);
            ViewBag.StatusOptions = new List<string> { "New", "Contacted", "Visited", "Enrolled", "Rejected" };
            ViewBag.SourceOptions = new List<string> { "Website", "Referral", "Walk-in", "Advertisement", "Social Media", "Other" };

            return View(admissionInquiry);
        }

        // GET: AdmissionInquiries/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var admissionInquiry = await _context.AdmissionInquiries
                .Include(a => a.ClassInterested)
                .Include(a => a.Campus)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (admissionInquiry == null)
            {
                return NotFound();
            }

            return View(admissionInquiry);
        }

        // POST: AdmissionInquiries/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var admissionInquiry = await _context.AdmissionInquiries.FindAsync(id);
            if (admissionInquiry != null)
            {
                admissionInquiry.IsActive = false; // Soft delete
                _context.Update(admissionInquiry);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Admission inquiry deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AdmissionInquiryExists(int id)
        {
            return _context.AdmissionInquiries.Any(e => e.Id == id);
        }

        // Helper method to create calendar event and todo for follow-up
        private async Task CreateFollowUpCalendarAndTodo(AdmissionInquiry inquiry)
        {
            if (!inquiry.FollowUpDate.HasValue) return;

            // Load navigation properties if not already loaded
            if (inquiry.ClassInterested == null)
            {
                await _context.Entry(inquiry).Reference(i => i.ClassInterested).LoadAsync();
            }

            // Get admin users
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var adminUser = adminUsers.FirstOrDefault(u => u.CampusId == inquiry.CampusId);
            
            if (adminUser == null)
            {
                // Fallback to any admin if no campus-specific admin found
                adminUser = adminUsers.FirstOrDefault();
            }

            if (adminUser != null)
            {
                var className = inquiry.ClassInterested?.Name ?? "N/A";
                
                // Create Calendar Event (visible only to admin)
                var calendarEvent = new CalendarEvent
                {
                    CampusId = inquiry.CampusId,
                    EventName = $"Follow-up: {inquiry.StudentName}",
                    Description = $"Follow-up for admission inquiry - Student: {inquiry.StudentName}, Class: {className}, Phone: {inquiry.PhoneNumber}",
                    StartDate = inquiry.FollowUpDate.Value,
                    EndDate = inquiry.FollowUpDate.Value,
                    IsHoliday = false,
                    IsActive = true,
                    CreatedBy = User.Identity?.Name,
                    CreatedAt = DateTime.Now
                };
                _context.CalendarEvents.Add(calendarEvent);

                // Create ToDo item for admin
                var todo = new ToDo
                {
                    Title = $"Follow-up with {inquiry.StudentName}",
                    Description = $"Contact regarding admission inquiry for {className}. Phone: {inquiry.PhoneNumber}",
                    UserId = adminUser.Id,
                    DueDate = inquiry.FollowUpDate.Value,
                    Priority = "High",
                    IsCompleted = false,
                    CampusId = inquiry.CampusId,
                    CreatedBy = User.Identity?.Name,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };
                _context.ToDos.Add(todo);

                await _context.SaveChangesAsync();
            }
        }

        // AJAX endpoint to update status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            // Validate status
            var validStatuses = new[] { "New", "Contacted", "Visited", "Enrolled", "Rejected" };
            if (string.IsNullOrEmpty(status) || !validStatuses.Contains(status))
            {
                return Json(new { success = false, message = "Invalid status value" });
            }

            var inquiry = await _context.AdmissionInquiries.FindAsync(id);
            if (inquiry == null)
            {
                return Json(new { success = false, message = "Inquiry not found" });
            }

            var oldStatus = inquiry.InquiryStatus;
            inquiry.InquiryStatus = status;
            _context.Update(inquiry);
            await _context.SaveChangesAsync();

            // If status changed to "Enrolled", remove follow-up calendar events and todos
            if (status == "Enrolled" && oldStatus != "Enrolled")
            {
                await RemoveFollowUpCalendarAndTodo(inquiry);
            }

            return Json(new { success = true, message = "Status updated successfully" });
        }

        // Helper method to remove calendar event and todo for follow-up
        private async Task RemoveFollowUpCalendarAndTodo(AdmissionInquiry inquiry)
        {
            // Remove calendar events related to this inquiry
            var calendarEvents = await _context.CalendarEvents
                .Where(ce => ce.IsActive && 
                            ce.EventName.Contains(inquiry.StudentName) && 
                            ce.EventName.Contains("Follow-up") &&
                            ce.CampusId == inquiry.CampusId)
                .ToListAsync();

            foreach (var calendarEvent in calendarEvents)
            {
                calendarEvent.IsActive = false;
                _context.Update(calendarEvent);
            }

            // Remove todo items related to this inquiry
            var todos = await _context.ToDos
                .Where(t => t.IsActive && 
                           t.Title.Contains(inquiry.StudentName) && 
                           t.Title.Contains("Follow-up") &&
                           t.CampusId == inquiry.CampusId)
                .ToListAsync();

            foreach (var todo in todos)
            {
                todo.IsActive = false;
                _context.Update(todo);
            }

            await _context.SaveChangesAsync();
        }
    }
}