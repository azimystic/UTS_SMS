using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using SMS.Services;

namespace SMS.Controllers
{
    [Authorize]
    public class LeaveRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserService _userService;

        public LeaveRequestsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IUserService userService)
        {
            _context = context;
            _userManager = userManager;
            _userService = userService;
        }

        // GET: LeaveRequests (Teacher View)
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.EmployeeId == null)
            {
                //return NotFound("Employee record not found.");
            }

            var leaveRequests = await _context.LeaveRequests
                .Where(lr => lr.EmployeeId == currentUser.EmployeeId)
                .Include(lr => lr.Employee)
                .OrderByDescending(lr => lr.CreatedAt)
                .ToListAsync();

            // Get leave balances
            var leaveBalances = await _context.LeaveBalances
                .Where(lb => lb.EmployeeId == currentUser.EmployeeId)
                .ToListAsync();

            ViewBag.LeaveBalances = leaveBalances;

            return View(leaveRequests);
        }

        // GET: LeaveRequests/AdminIndex (Admin View)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminIndex(string statusFilter = "Pending")
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            var leaveRequestsQuery = _context.LeaveRequests
                .Include(lr => lr.Employee)
                .AsQueryable();

            // Filter by campus
            if (campusId != null && campusId != 0)
            {
                leaveRequestsQuery = leaveRequestsQuery.Where(lr => lr.CampusId == campusId);
            }

            // Filter by status
            if (!string.IsNullOrEmpty(statusFilter))
            {
                leaveRequestsQuery = leaveRequestsQuery.Where(lr => lr.Status == statusFilter);
            }

            var leaveRequests = await leaveRequestsQuery
                .OrderByDescending(lr => lr.CreatedAt)
                .ToListAsync();

            ViewBag.StatusFilter = statusFilter;
            return View(leaveRequests);
        }

        // GET: LeaveRequests/Create
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.EmployeeId == null)
            {
                TempData["ErrorMessage"] = "Employee record not found.";
                return RedirectToAction(nameof(Index));
            }

            // Get leave balances for this employee
            var leaveBalances = await _context.LeaveBalances
                .Where(lb => lb.EmployeeId == currentUser.EmployeeId && lb.Year == DateTime.Now.Year)
                .ToListAsync();

            ViewBag.LeaveBalances = leaveBalances;
            
            // Get approved leave history
            var approvedLeaves = await _context.LeaveRequests
                .Where(lr => lr.EmployeeId == currentUser.EmployeeId && lr.Status == "Approved")
                .OrderByDescending(lr => lr.StartDate)
                .Take(10)
                .ToListAsync();
            
            ViewBag.ApprovedLeaves = approvedLeaves;
            
            ViewData["LeaveTypes"] = new SelectList(new[]
            {
                "Sick Leave", "Casual Leave", "Annual Leave", "Emergency Leave", "Maternity Leave", "Paternity Leave"
            });

            return View();
        }

        // Helper method to calculate working days excluding Sundays and holidays
        private async Task<int> CalculateWorkingDays(DateTime startDate, DateTime endDate, int campusId)
        {
            // Get holidays for the date range
            var holidays = await _context.CalendarEvents
     .Where(ce => ce.IsActive &&
                  ce.IsHoliday &&
                  ce.CampusId == campusId &&
                  // The holiday must start before your range ends
                  ce.StartDate <= endDate &&
                  // The holiday must end after your range starts
                  (ce.EndDate == null ? ce.StartDate >= startDate : ce.EndDate >= startDate))
     .ToListAsync();

            int workingDays = 0;
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                // Exclude Sundays
                if (date.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                // Check if date is a holiday
                bool isHoliday = holidays.Any(h => 
                    date.Date >= h.StartDate.Date && 
                    (h.EndDate == null || date.Date <= h.EndDate.Value.Date));

                if (!isHoliday)
                {
                    workingDays++;
                }
            }

            return workingDays;
        }

        // POST: LeaveRequests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Create([Bind("StartDate,EndDate,LeaveType,Reason")] LeaveRequest leaveRequest)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.EmployeeId == null)
            {
                return NotFound("Employee record not found.");
            }
            ModelState.Remove("Campus");
            ModelState.Remove("Employee");
            if (ModelState.IsValid)
            {
                // Validate dates
                if (leaveRequest.StartDate > leaveRequest.EndDate)
                {
                    ModelState.AddModelError("EndDate", "End date must be after start date.");
                    ViewData["LeaveTypes"] = new SelectList(new[] { "Sick Leave", "Casual Leave", "Annual Leave", "Emergency Leave", "Maternity Leave", "Paternity Leave" });
                    return View(leaveRequest);
                }

                // Calculate working days excluding Sundays and holidays
                var campusId = currentUser.CampusId ?? 0;
                var totalWorkingDays = await CalculateWorkingDays(leaveRequest.StartDate, leaveRequest.EndDate, campusId);

                // Check if employee has sufficient leave balance
                var leaveBalance = await _context.LeaveBalances
                    .FirstOrDefaultAsync(lb => lb.EmployeeId == currentUser.EmployeeId 
                        && lb.LeaveType == leaveRequest.LeaveType 
                        && lb.Year == DateTime.Now.Year);

                if (leaveBalance != null && leaveBalance.Available < totalWorkingDays)
                {
                    ModelState.AddModelError("", $"Insufficient leave balance. Available: {leaveBalance.Available} days, Requested: {totalWorkingDays} days");
                    ViewData["LeaveTypes"] = new SelectList(new[] { "Sick Leave", "Casual Leave", "Annual Leave", "Emergency Leave", "Maternity Leave", "Paternity Leave" });
                    ViewBag.LeaveBalances = await _context.LeaveBalances
                        .Where(lb => lb.EmployeeId == currentUser.EmployeeId && lb.Year == DateTime.Now.Year)
                        .ToListAsync();
                    ViewBag.ApprovedLeaves = await _context.LeaveRequests
                        .Where(lr => lr.EmployeeId == currentUser.EmployeeId && lr.Status == "Approved")
                        .OrderByDescending(lr => lr.StartDate)
                        .Take(10)
                        .ToListAsync();
                    return View(leaveRequest);
                }

                leaveRequest.EmployeeId = currentUser.EmployeeId.Value;
                leaveRequest.CampusId = currentUser.CampusId ?? 0;
                leaveRequest.CreatedBy = currentUser.UserName;
                leaveRequest.CreatedAt = DateTime.Now;
                leaveRequest.Status = "Pending";

                _context.Add(leaveRequest);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Leave request submitted successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["LeaveTypes"] = new SelectList(new[] { "Sick Leave", "Casual Leave", "Annual Leave", "Emergency Leave", "Maternity Leave", "Paternity Leave" });
            ViewBag.LeaveBalances = await _context.LeaveBalances
                .Where(lb => lb.EmployeeId == currentUser.EmployeeId && lb.Year == DateTime.Now.Year)
                .ToListAsync();
            ViewBag.ApprovedLeaves = await _context.LeaveRequests
                .Where(lr => lr.EmployeeId == currentUser.EmployeeId && lr.Status == "Approved")
                .OrderByDescending(lr => lr.StartDate)
                .Take(10)
                .ToListAsync();
            return View(leaveRequest);
        }

        // POST: LeaveRequests/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var leaveRequest = await _context.LeaveRequests
                .Include(lr => lr.Employee)
                .FirstOrDefaultAsync(lr => lr.Id == id);

            if (leaveRequest == null)
            {
                return NotFound();
            }

            if (leaveRequest.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Only pending requests can be approved.";
                return RedirectToAction(nameof(AdminIndex));
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Calculate actual working days excluding Sundays and holidays
            var totalWorkingDays = await CalculateWorkingDays(leaveRequest.StartDate, leaveRequest.EndDate, leaveRequest.CampusId);

            // Update leave request status
            leaveRequest.Status = "Approved";
            leaveRequest.ApprovedBy = currentUser?.UserName;
            leaveRequest.ApprovedAt = DateTime.Now;
            leaveRequest.UpdatedBy = currentUser?.UserName;
            leaveRequest.UpdatedAt = DateTime.Now;

            // Deduct from leave balance
            var leaveBalance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.EmployeeId == leaveRequest.EmployeeId 
                    && lb.LeaveType == leaveRequest.LeaveType 
                    && lb.Year == DateTime.Now.Year);

            if (leaveBalance != null)
            {
                var balanceBefore = leaveBalance.Available;
                leaveBalance.Used += totalWorkingDays;
                leaveBalance.UpdatedAt = DateTime.Now;
                leaveBalance.UpdatedBy = currentUser?.UserName;

                // Record history
                var history = new LeaveBalanceHistory
                {
                    EmployeeId = leaveRequest.EmployeeId,
                    LeaveType = leaveRequest.LeaveType,
                    ActionType = "Used",
                    Amount = totalWorkingDays,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = leaveBalance.Available,
                    LeaveRequestId = leaveRequest.Id,
                    Remarks = $"Leave approved for {totalWorkingDays} days from {leaveRequest.StartDate:yyyy-MM-dd} to {leaveRequest.EndDate:yyyy-MM-dd}",
                    CreatedBy = currentUser?.UserName,
                    CreatedAt = DateTime.Now,
                    CampusId = leaveRequest.CampusId
                };
                _context.LeaveBalanceHistories.Add(history);
            }

            // Mark leaves in EmployeeAttendance (excluding Sundays and holidays)
            var holidays = await _context.CalendarEvents
                .Where(ce => ce.IsActive && 
                            ce.IsHoliday && 
                            ce.CampusId == leaveRequest.CampusId &&
                            ce.StartDate <= leaveRequest.EndDate &&
                            (ce.EndDate == null || ce.EndDate >= leaveRequest.StartDate))
                .ToListAsync();

            for (var date = leaveRequest.StartDate; date <= leaveRequest.EndDate; date = date.AddDays(1))
            {
                // Skip Sundays
                if (date.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                // Skip holidays
                bool isHoliday = holidays.Any(h => 
                    date.Date >= h.StartDate.Date && 
                    (h.EndDate == null || date.Date <= h.EndDate.Value.Date));
                
                if (isHoliday)
                    continue;

                var existingAttendance = await _context.EmployeeAttendance
                    .FirstOrDefaultAsync(ea => ea.EmployeeId == leaveRequest.EmployeeId && ea.Date.Date == date.Date);

                if (existingAttendance != null)
                {
                    existingAttendance.Status = "L";
                    existingAttendance.Remarks = $"Leave: {leaveRequest.LeaveType}";
                    existingAttendance.UpdatedBy = currentUser?.UserName;
                    existingAttendance.UpdatedAt = DateTime.Now;
                }
                else
                {
                    var attendance = new EmployeeAttendance
                    {
                        EmployeeId = leaveRequest.EmployeeId,
                        Date = date,
                        Status = "L",
                        Remarks = $"Leave: {leaveRequest.LeaveType}",
                        CreatedBy = currentUser?.UserName,
                        CreatedAt = DateTime.Now
                    };
                    _context.EmployeeAttendance.Add(attendance);
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Leave request approved successfully! {totalWorkingDays} days marked as leave.";
            return RedirectToAction(nameof(AdminIndex));
        }

        // POST: LeaveRequests/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int id, string rejectionReason)
        {
            var leaveRequest = await _context.LeaveRequests.FindAsync(id);
            if (leaveRequest == null)
            {
                return NotFound();
            }

            if (leaveRequest.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Only pending requests can be rejected.";
                return RedirectToAction(nameof(AdminIndex));
            }

            var currentUser = await _userManager.GetUserAsync(User);
            leaveRequest.Status = "Rejected";
            leaveRequest.RejectionReason = rejectionReason;
            leaveRequest.UpdatedBy = currentUser?.UserName;
            leaveRequest.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Leave request rejected successfully!";
            return RedirectToAction(nameof(AdminIndex));
        }

        private bool LeaveRequestExists(int id)
        {
            return _context.LeaveRequests.Any(e => e.Id == id);
        }
    }
}
