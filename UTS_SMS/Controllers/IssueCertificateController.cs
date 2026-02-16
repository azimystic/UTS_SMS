using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using UTS_SMS.Services;

namespace UTS_SMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class IssueCertificateController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ReportService _reportService;

        public IssueCertificateController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            ReportService reportService)
        {
            _context = context;
            _userManager = userManager;
            _reportService = reportService;
        }

        // GET: IssueCertificate
        public async Task<IActionResult> Index(string searchString = "", int? certificateFilter = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            ViewBag.CertificateTypes = await _context.CertificateTypes
                .Where(ct => ct.IsActive && (campusId == null || ct.CampusId == campusId))
                .OrderBy(ct => ct.CertificateName)
                .ToListAsync();

            ViewData["SearchString"] = searchString;
            ViewData["CertificateFilter"] = certificateFilter;

            var query = _context.CertificateRequests
                .Include(cr => cr.Student)
                    .ThenInclude(s => s.ClassObj)
                .Include(cr => cr.Student)
                    .ThenInclude(s => s.SectionObj)
                .Include(cr => cr.CertificateType)
                .Include(cr => cr.GeneratedFine)
                .Where(cr => cr.IsActive && (campusId == null || cr.CampusId == campusId));

            // Apply search
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(cr =>
                    cr.Student.StudentName.Contains(searchString) ||
                    cr.Student.StudentCNIC.Contains(searchString) ||
                    cr.Student.RollNumber.Contains(searchString));
            }

            // Apply certificate type filter
            if (certificateFilter.HasValue)
            {
                query = query.Where(cr => cr.CertificateTypeId == certificateFilter.Value);
            }

            var certificateRequests = await query
                .OrderByDescending(cr => cr.IssueDate)
                .ToListAsync();

            return View(certificateRequests);
        }

        // GET: IssueCertificate/Create
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            ViewBag.CertificateTypes = await _context.CertificateTypes
                .Where(ct => ct.IsActive && (campusId == null || ct.CampusId == campusId))
                .OrderBy(ct => ct.CertificateName)
                .ToListAsync();

            return View();
        }

        // POST: IssueCertificate/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int studentId, int certificateTypeId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            // Validate student
            var student = await _context.Students
                .Where(s => s.Id == studentId && (campusId == null || s.CampusId == campusId))
                .FirstOrDefaultAsync();

            if (student == null)
            {
                TempData["ErrorMessage"] = "Student not found.";
                return RedirectToAction(nameof(Create));
            }

            // Validate certificate type
            var certificateType = await _context.CertificateTypes
                .Where(ct => ct.Id == certificateTypeId && ct.IsActive && (campusId == null || ct.CampusId == campusId))
                .FirstOrDefaultAsync();

            if (certificateType == null)
            {
                TempData["ErrorMessage"] = "Certificate type not found.";
                return RedirectToAction(nameof(Create));
            }

            // Create fine charge
            var fineCharge = new StudentFineCharge
            {
                StudentId = studentId,
                ChargeName = certificateType.CertificateName,
                Amount = certificateType.Price,
                Description = $"Certificate Fee - {certificateType.CertificateName}",
                ChargeDate = DateTime.Now,
                IsPaid = false,
                CampusId = campusId ?? 0,
                CreatedBy = currentUser.UserName,
                CreatedDate = DateTime.Now,
                IsActive = true
            };

            _context.StudentFineCharges.Add(fineCharge);
            await _context.SaveChangesAsync();

            // Create certificate request
            var certificateRequest = new CertificateRequest
            {
                StudentId = studentId,
                CertificateTypeId = certificateTypeId,
                IssueDate = DateTime.Now,
                IsPaid = false,
                GeneratedFineId = fineCharge.Id,
                CampusId = campusId ?? 0,
                CreatedBy = currentUser.UserName,
                CreatedDate = DateTime.Now,
                IsActive = true
            };

            _context.CertificateRequests.Add(certificateRequest);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Certificate request created successfully for {student.StudentName}. Fine of {certificateType.Price:C} has been added.";
            return RedirectToAction(nameof(Index));
        }

        // GET: IssueCertificate/SearchStudent
        [HttpGet]
        public async Task<IActionResult> SearchStudent(string term)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            if (string.IsNullOrWhiteSpace(term))
            {
                return Json(new List<object>());
            }

            var students = await _context.Students
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .Where(s => !s.HasLeft &&
                           (campusId == null || s.CampusId == campusId) &&
                           (s.StudentName.Contains(term) || 
                            s.RollNumber.Contains(term) ||
                            s.StudentCNIC.Contains(term)))
                .OrderBy(s => s.StudentName)
                .Take(10)
                .Select(s => new
                {
                    id = s.Id,
                    name = s.StudentName,
                    rollNumber = s.RollNumber,
                    className = s.ClassObj != null ? s.ClassObj.Name : "",
                    sectionName = s.SectionObj != null ? s.SectionObj.Name : "",
                    cnic = s.StudentCNIC
                })
                .ToListAsync();

            return Json(students);
        }

        // POST: IssueCertificate/Print
        [HttpPost]
        public async Task<IActionResult> Print(int id, bool forcePrint = false)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            var certificateRequest = await _context.CertificateRequests
                .Include(cr => cr.Student)
                .Include(cr => cr.CertificateType)
                .Include(cr => cr.GeneratedFine)
                .Where(cr => cr.Id == id && (campusId == null || cr.CampusId == campusId))
                .FirstOrDefaultAsync();

            if (certificateRequest == null)
            {
                return NotFound();
            }

            // Check payment status
            bool isPaid = certificateRequest.GeneratedFine?.IsPaid ?? false;

            if (!isPaid && !forcePrint)
            {
                return Json(new { success = false, message = "Fee not paid." });
            }

            try
            {
                // Prepare placeholders
                var placeholders = new Dictionary<string, string>
                {
                    { "student_name", certificateRequest.Student.StudentName ?? "" },
                    { "rollnumber", certificateRequest.Student.RollNumber ?? "" },
                    { "roll_number", certificateRequest.Student.RollNumber ?? "" },
                    { "cnic", certificateRequest.Student.StudentCNIC ?? "" },
                    { "student_cnic", certificateRequest.Student.StudentCNIC ?? "" },
                    { "father_name", certificateRequest.Student.FatherName ?? "" },
                    { "father_cnic", certificateRequest.Student.FatherCNIC ?? "" },
                    { "date", DateTime.Now.ToString("dd-MM-yyyy") },
                    { "issue_date", certificateRequest.IssueDate.ToString("dd-MM-yyyy") },
                    { "certificate_name", certificateRequest.CertificateType.CertificateName ?? "" }
                };

                // Generate PDF
                var pdfBytes = await _reportService.GeneratePdfFromTemplate(
                    certificateRequest.CertificateType.ReportFileName,
                    placeholders);

                var fileName = $"{certificateRequest.CertificateType.CertificateName}_{certificateRequest.Student.StudentName}_{DateTime.Now:yyyyMMdd}.pdf";
                // Sanitize filename
                var invalidChars = Path.GetInvalidFileNameChars();
                fileName = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
                
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error generating certificate: {ex.Message}" });
            }
        }

        // POST: IssueCertificate/CheckPaymentStatus
        [HttpPost]
        public async Task<IActionResult> CheckPaymentStatus(int id)
        {
            var certificateRequest = await _context.CertificateRequests
                .Include(cr => cr.GeneratedFine)
                .Where(cr => cr.Id == id)
                .FirstOrDefaultAsync();

            if (certificateRequest == null)
            {
                return Json(new { success = false, message = "Certificate request not found." });
            }

            bool isPaid = certificateRequest.GeneratedFine?.IsPaid ?? false;

            return Json(new { success = true, isPaid = isPaid });
        }
    }
}
