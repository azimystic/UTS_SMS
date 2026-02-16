using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;

namespace UTS_SMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AssetController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AssetController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Asset
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            IQueryable<Asset> assetsQuery = _context.Assets
                .Include(a => a.Account)
                .Include(a => a.Campus)
                .Where(a => a.IsActive);

            if (campusId.HasValue && campusId > 0)
            {
                assetsQuery = assetsQuery.Where(a => a.CampusId == campusId);
            }

            var assets = await assetsQuery.OrderByDescending(a => a.CreatedDate).ToListAsync();

            ViewBag.Campuses = campusId.HasValue && campusId > 0 
                ? await _context.Campuses.Where(c => c.IsActive && c.Id == campusId).ToListAsync()
                : await _context.Campuses.Where(c => c.IsActive).ToListAsync();

            return View(assets);
        }

        // GET: Asset/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var asset = await _context.Assets
                .Include(a => a.Account)
                .Include(a => a.Campus)
                .FirstOrDefaultAsync(a => a.Id == id && a.IsActive);

            if (asset == null)
                return NotFound();

            return View(asset);
        }

        // GET: Asset/Create
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            if (campusId.HasValue && campusId > 0)
            {
                ViewData["AccountId"] = new SelectList(_context.BankAccounts.Where(b => b.IsActive && b.CampusId == campusId), "Id", "AccountTitle");
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive && c.Id == campusId), "Id", "Name");
            }
            else
            {
                ViewData["AccountId"] = new SelectList(_context.BankAccounts.Where(b => b.IsActive), "Id", "AccountTitle");
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name");
            }

            return View();
        }

        // POST: Asset/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Price,PurchaseDate,AccountId,SerialNumber,Category,Condition,CampusId")] Asset asset)
        {
            ModelState.Remove("Account");
            ModelState.Remove("Campus");

            if (ModelState.IsValid)
            {
                asset.CreatedDate = DateTime.Now;
                asset.CreatedBy = User.Identity?.Name;

                _context.Add(asset);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Asset created successfully!";
                return RedirectToAction(nameof(Index));
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            if (campusId.HasValue && campusId > 0)
            {
                ViewData["AccountId"] = new SelectList(_context.BankAccounts.Where(b => b.IsActive && b.CampusId == campusId), "Id", "AccountTitle", asset.AccountId);
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive && c.Id == campusId), "Id", "Name", asset.CampusId);
            }
            else
            {
                ViewData["AccountId"] = new SelectList(_context.BankAccounts.Where(b => b.IsActive), "Id", "AccountTitle", asset.AccountId);
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name", asset.CampusId);
            }

            return View(asset);
        }

        // GET: Asset/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var asset = await _context.Assets.FindAsync(id);
            if (asset == null || !asset.IsActive)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            if (campusId.HasValue && campusId > 0)
            {
                ViewData["AccountId"] = new SelectList(_context.BankAccounts.Where(b => b.IsActive && b.CampusId == campusId), "Id", "AccountTitle", asset.AccountId);
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive && c.Id == campusId), "Id", "Name", asset.CampusId);
            }
            else
            {
                ViewData["AccountId"] = new SelectList(_context.BankAccounts.Where(b => b.IsActive), "Id", "AccountTitle", asset.AccountId);
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name", asset.CampusId);
            }

            return View(asset);
        }

        // POST: Asset/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,PurchaseDate,AccountId,SerialNumber,Category,Condition,CampusId")] Asset asset)
        {
            if (id != asset.Id)
                return NotFound();

            ModelState.Remove("Account");
            ModelState.Remove("Campus");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingAsset = await _context.Assets.FindAsync(id);
                    if (existingAsset == null)
                        return NotFound();

                    existingAsset.Name = asset.Name;
                    existingAsset.Description = asset.Description;
                    existingAsset.Price = asset.Price;
                    existingAsset.PurchaseDate = asset.PurchaseDate;
                    existingAsset.AccountId = asset.AccountId;
                    existingAsset.SerialNumber = asset.SerialNumber;
                    existingAsset.Category = asset.Category;
                    existingAsset.Condition = asset.Condition;
                    existingAsset.ModifiedDate = DateTime.Now;
                    existingAsset.ModifiedBy = User.Identity?.Name;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AssetExists(asset.Id))
                        return NotFound();
                    else
                        throw;
                }

                TempData["SuccessMessage"] = "Asset updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            if (campusId.HasValue && campusId > 0)
            {
                ViewData["AccountId"] = new SelectList(_context.BankAccounts.Where(b => b.IsActive && b.CampusId == campusId), "Id", "AccountTitle", asset.AccountId);
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive && c.Id == campusId), "Id", "Name", asset.CampusId);
            }
            else
            {
                ViewData["AccountId"] = new SelectList(_context.BankAccounts.Where(b => b.IsActive), "Id", "AccountTitle", asset.AccountId);
                ViewData["CampusId"] = new SelectList(_context.Campuses.Where(c => c.IsActive), "Id", "Name", asset.CampusId);
            }

            return View(asset);
        }

        // POST: Asset/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset != null)
            {
                asset.IsActive = false;
                asset.ModifiedDate = DateTime.Now;
                asset.ModifiedBy = User.Identity?.Name;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Asset deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AssetExists(int id)
        {
            return _context.Assets.Any(e => e.Id == id);
        }
    }
}