using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;

namespace UTS_SMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CertificateSettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CertificateSettingsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: CertificateSettings
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            var certificateTypes = await _context.CertificateTypes
                .Include(ct => ct.Campus)
                .Where(ct => ct.IsActive && (campusId == null || ct.CampusId == campusId))
                .OrderBy(ct => ct.CertificateName)
                .ToListAsync();

            return View(certificateTypes);
        }

        // GET: CertificateSettings/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CertificateSettings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CertificateName,Price,ReportFileName")] CertificateType certificateType)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                
                certificateType.CampusId = currentUser.CampusId ?? 0;
                certificateType.CreatedBy = currentUser.UserName;
                certificateType.CreatedDate = DateTime.Now;
                certificateType.IsActive = true;

                _context.Add(certificateType);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Certificate type created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(certificateType);
        }

        // GET: CertificateSettings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            var certificateType = await _context.CertificateTypes
                .Where(ct => ct.Id == id && (campusId == null || ct.CampusId == campusId))
                .FirstOrDefaultAsync();

            if (certificateType == null)
            {
                return NotFound();
            }

            return View(certificateType);
        }

        // POST: CertificateSettings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CertificateName,Price,ReportFileName,CampusId,CreatedBy,CreatedDate,IsActive")] CertificateType certificateType)
        {
            if (id != certificateType.Id)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            // Verify campus access
            if (campusId != null && certificateType.CampusId != campusId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    certificateType.ModifiedBy = currentUser.UserName;
                    certificateType.ModifiedDate = DateTime.Now;

                    _context.Update(certificateType);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Certificate type updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CertificateTypeExists(certificateType.Id))
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
            return View(certificateType);
        }

        // POST: CertificateSettings/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser.CampusId;

            var certificateType = await _context.CertificateTypes
                .Where(ct => ct.Id == id && (campusId == null || ct.CampusId == campusId))
                .FirstOrDefaultAsync();

            if (certificateType == null)
            {
                return NotFound();
            }

            // Soft delete
            certificateType.IsActive = false;
            certificateType.ModifiedBy = currentUser.UserName;
            certificateType.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Certificate type deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private bool CertificateTypeExists(int id)
        {
            return _context.CertificateTypes.Any(e => e.Id == id);
        }
    }
}
