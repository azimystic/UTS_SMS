using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using SMS.Services;
using Microsoft.AspNetCore.Hosting;

namespace SMS.Controllers
{
    public class CampusesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;

        public CampusesController(ApplicationDbContext context, IUserService userService, IWebHostEnvironment env, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userService = userService;
            _env = env;
            _userManager = userManager;
        }

        // GET: List all campuses
        public async Task<IActionResult> Index()
        {
            return View(await _context.Campuses.Where(x => x.IsActive == true).ToListAsync());
        }

        // GET: Details of specific campus
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var campus = await _context.Campuses.FindAsync(id);
            if (campus == null || !campus.IsActive)
            {
                return NotFound();
            }

            return View(campus);
        }

        // GET: Create form
        public IActionResult Create()
        {
            ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name");
            return View();
        }

        // POST: Create new campus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Campus campus, IFormFile? Logo)
        {
            ModelState.Remove("Classes");
            ModelState.Remove("Campus");
            ModelState.Remove("ClassSections");
            ModelState.Remove("TeacherAssignments");
            ModelState.Remove("Class");
            ModelState.Remove("Logo");

            if (ModelState.IsValid)
            {
                if (Logo != null && Logo.Length > 0)
                {
                    campus.Logo = await UploadFile(Logo, "campusData/Logo");
                }

                campus.IsActive = true;
                _context.Add(campus);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name", campus.Id);
            return View(campus);
        }

        // GET: Edit form
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var campus = await _context.Campuses.FindAsync(id);
            if (campus == null || !campus.IsActive)
            {
                return NotFound();
            }

            ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name", campus.Id);
            return View(campus);
        }

        // POST: Update campus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Campus campus, IFormFile? Logo)
        {
            if (id != campus.Id)
            {
                return NotFound();
            }

            ModelState.Remove("Classes");
            ModelState.Remove("Campus");
            ModelState.Remove("ClassSections");
            ModelState.Remove("TeacherAssignments");
            ModelState.Remove("Logo");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingCampus = await _context.Campuses.FindAsync(id);
                    if (existingCampus == null)
                    {
                        return NotFound();
                    }

                    // Update properties
                    existingCampus.Name = campus.Name;
                    existingCampus.Address = campus.Address;
                    existingCampus.Phone = campus.Phone;
                    existingCampus.Email = campus.Email;
                    existingCampus.IsActive = campus.IsActive;
                    existingCampus.StartTime = campus.StartTime;
                    existingCampus.EndTime = campus.EndTime;

                    // Handle logo upload
                    if (Logo != null && Logo.Length > 0)
                    {
                        // Delete old logo if exists
                        if (!string.IsNullOrEmpty(existingCampus.Logo))
                        {
                            DeleteFile(existingCampus.Logo);
                        }
                        existingCampus.Logo = await UploadFile(Logo, "campusData/Logo");
                    }

                    _context.Update(existingCampus);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await CampusExists(campus.Id))
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

            ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name", campus.Id);
            return View(campus);
        }

        // GET: Delete confirmation
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var campus = await _context.Campuses.FindAsync(id);
            if (campus == null || !campus.IsActive)
            {
                return NotFound();
            }

            return View(campus);
        }

        // POST: Soft delete campus
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var campus = await _context.Campuses.FindAsync(id);
            if (campus != null)
            {
                campus.IsActive = false;
                _context.Update(campus);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> CampusExists(int id)
        {
            return await _context.Campuses.AnyAsync(e => e.Id == id && e.IsActive);
        }

        private async Task<string> UploadFile(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
                return string.Empty;

            // Create directory if it doesn't exist
            var uploadPath = Path.Combine(_env.WebRootPath, folderName);
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // Generate unique filename
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadPath, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative path
            return $"{folderName}/{fileName}";
        }

        private void DeleteFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            var fullPath = Path.Combine(_env.WebRootPath, filePath);
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}