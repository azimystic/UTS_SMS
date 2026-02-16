using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using SMS.Services;
using SMS.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Controllers
{
    public class ClassFeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClassFeeController(ApplicationDbContext context, IUserService userService, IWebHostEnvironment env, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userService = userService;
            _env = env;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int? campusId, int? classId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser?.CampusId;

            var query = _context.ClassFees
                .Include(cf => cf.Class)
                    .ThenInclude(c => c.Campus)
                .AsQueryable();


            // 🔹 Campus filter list
            if (userCampusId != null && userCampusId != 0)
            {
                // Restrict to user’s campus
                ViewData["CampusId"] = new SelectList(
                    _context.Campuses.Where(cs => cs.IsActive && cs.Id == userCampusId),
                    "Id", "Name", campusId ?? userCampusId
                );
                  query = _context.ClassFees
                    .Where(cf => cf.CampusId == userCampusId)
              .Include(cf => cf.Class)
                  .ThenInclude(c => c.Campus)
              .AsQueryable();

            }
            else
            {
                // Show all campuses
                ViewData["CampusId"] = new SelectList(
                    _context.Campuses.Where(cs => cs.IsActive),
                    "Id", "Name", campusId
                );
                  query = _context.ClassFees
              .Include(cf => cf.Class)
                  .ThenInclude(c => c.Campus)
              .AsQueryable();
            }

            // 🔹 Class filter list
            if (userCampusId == null || userCampusId == 0)
            {
                // Show all classes
                ViewData["ClassId"] = new SelectList(
                    _context.Classes.Include(c => c.Campus).Where(c => c.IsActive),
                    "Id", "Name", classId
                );
            }
            else
            {
                // Restrict to classes of user’s campus
                ViewData["ClassId"] = new SelectList(
                    _context.Classes.Include(c => c.Campus)
                        .Where(c => c.IsActive && c.CampusId == userCampusId),
                    "Id", "Name", classId
                );
            }

            // Apply filter
            if (campusId != null && campusId != 0)
                query = query.Where(cf => cf.CampusId == campusId);

            if (classId != null && classId != 0)
                query = query.Where(cf => cf.ClassId == classId);
            return View(await query.ToListAsync());
        }




        // GET: ClassFee/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var classFee = await _context.ClassFees
                .Include(cf => cf.Class)
                .ThenInclude(c => c.Campus)
                .FirstOrDefaultAsync(m => m.ClassId == id);

            if (classFee == null)
            {
                return NotFound();
            }

            return View(classFee);
        }

        // GET: ClassFee/Create
        // GET: ClassFee/Create
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            // Get IDs of classes that already have fees
            var classIdsWithFees = await _context.ClassFees
                .Select(cf => cf.ClassId)
                .ToListAsync();

            // 🔹 Campus dropdown
            if (campusId == null || campusId == 0)
            {
                // Show all active campuses (for admin)
                ViewData["CampusId"] = new SelectList(
                    _context.Campuses.Where(cs => cs.IsActive),
                    "Id", "Name"
                );

                // Show only active classes that don't have fees yet
                ViewBag.Classes = await _context.Classes
                    .Include(c => c.Campus)
                    .Where(c => c.IsActive && !classIdsWithFees.Contains(c.Id))
                    .ToListAsync();
            }
            else
            {
                // Restrict to user's campus only
                ViewData["CampusId"] = new SelectList(
                    _context.Campuses.Where(cs => cs.IsActive && cs.Id == campusId),
                    "Id", "Name", campusId
                );

                // Restrict to classes of user's campus only that don't have fees yet
                ViewBag.Classes = await _context.Classes
                    .Include(c => c.Campus)
                    .Where(c => c.IsActive && c.CampusId == campusId && !classIdsWithFees.Contains(c.Id))
                    .ToListAsync();
            }

            return View();
        }



        // POST: ClassFee/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClassId,TuitionFee,AdmissionFee,CreatedBy,ModifiedBy")] ClassFee classFee,
            List<ClassFeeExtraChargeViewModel> ExtraCharges,
            string applyOption,
            List<int> SelectedClassIds)
        {
            ModelState.Remove("Class");
            ModelState.Remove("CreatedBy");
            ModelState.Remove("ModifiedBy");
            ModelState.Remove("Campus");
            
            // Remove SelectedStudentIds from ModelState validation
            var keysToRemove = ModelState.Keys
                .Where(k => k.Contains(".SelectedStudentIds"))
                .ToList();
            
            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            }
            
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            if (ModelState.IsValid)
            {
                int finalCampusId;

                if (campusId == null || campusId == 0)
                {
                    var selectedClass = await _context.Classes
                        .FirstOrDefaultAsync(c => c.Id == classFee.ClassId);

                    if (selectedClass == null)
                    {
                        ModelState.AddModelError("ClassId", "Invalid class selected.");
                        return View(classFee);
                    }

                    finalCampusId = selectedClass.CampusId;
                }
                else
                {
                    finalCampusId = campusId.Value;
                }

                var existingFee = await _context.ClassFees
                    .FirstOrDefaultAsync(cf => cf.ClassId == classFee.ClassId && cf.CampusId == finalCampusId);

                if (existingFee != null)
                {
                    ModelState.AddModelError("ClassId", "Fee structure already exists for this class.");

                    if (campusId == null || campusId == 0)
                    {
                        ViewBag.Classes = _context.Classes
                            .Include(c => c.Campus)
                            .Where(c => c.IsActive)
                            .ToList();
                    }
                    else
                    {
                        ViewBag.Classes = _context.Classes
                            .Include(c => c.Campus)
                            .Where(c => c.IsActive && c.CampusId == campusId)
                            .ToList();
                    }

                    return View(classFee);
                }

                classFee.CampusId = finalCampusId;
                classFee.CreatedDate = DateTime.Now;
                classFee.ModifiedDate = DateTime.Now;
                classFee.CreatedBy = User.Identity?.Name;
                classFee.ModifiedBy = User.Identity?.Name;

                _context.Add(classFee);
                await _context.SaveChangesAsync();
                
                // Save extra charges
                if (ExtraCharges != null && ExtraCharges.Any())
                {
                    // Get all students in the class for default selection
                    var allStudentsInClass = await _context.Students
                        .Where(s => s.Class == classFee.ClassId && !s.HasLeft)
                        .Select(s => s.Id)
                        .ToListAsync();
                    
                    foreach (var charge in ExtraCharges)
                    {
                        // If no students are selected, default to all students in the class
                        if (charge.SelectedStudentIds == null || !charge.SelectedStudentIds.Any())
                        {
                            charge.SelectedStudentIds = allStudentsInClass;
                        }
                    }
                    
                    await SaveExtraCharges(classFee.ClassId, finalCampusId, ExtraCharges);
                }
                
                // Apply to other classes if requested
                if (applyOption == "all" || (applyOption == "selected" && SelectedClassIds != null && SelectedClassIds.Any()))
                {
                    var classIdsToApply = applyOption == "all" 
                        ? await _context.Classes.Where(c => c.CampusId == finalCampusId && c.Id != classFee.ClassId).Select(c => c.Id).ToListAsync()
                        : SelectedClassIds.Where(id => id != classFee.ClassId).ToList();
                    
                    foreach (var classId in classIdsToApply)
                    {
                        await ApplyFeeStructureToClass(classFee, classId, finalCampusId, ExtraCharges);
                    }
                }
                
                TempData["Success"] = "Fee structure created successfully!";
                return RedirectToAction(nameof(Index));
            }

            // Repopulate dropdowns when validation fails
            if (campusId == null || campusId == 0)
            {
                ViewBag.Classes = _context.Classes
                    .Include(c => c.Campus)
                    .Where(c => c.IsActive)
                    .ToList();
            }
            else
            {
                ViewBag.Classes = _context.Classes
                    .Include(c => c.Campus)
                    .Where(c => c.IsActive && c.CampusId == campusId)
                    .ToList();
            }

            return View(classFee);
        }


        // GET: ClassFee/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var classFee = await _context.ClassFees.FindAsync(id);
            if (classFee == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            if (campusId == null || campusId == 0)
            {
                // Admin → all active classes
                ViewBag.Classes = _context.Classes
                    .Include(c => c.Campus)
                    .Where(c => c.IsActive)
                    .ToList();
            }
            else
            {
                // Campus user → only their campus classes
                ViewBag.Classes = _context.Classes
                    .Include(c => c.Campus)
                    .Where(c => c.IsActive && c.CampusId == campusId)
                    .ToList();
            }

            return View(classFee);
        }

        // POST: ClassFee/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClassId,TuitionFee,AdmissionFee,ModifiedBy")] ClassFee classFee,
            List<ClassFeeExtraChargeViewModel> ExtraCharges,
            List<int> DeletedChargeIds,
            string applyOption,
            List<int> SelectedClassIds)
        {
            if (id != classFee.Id)
            {
                return NotFound();
            }

            ModelState.Remove("Class");
            ModelState.Remove("CreatedBy");
            ModelState.Remove("ModifiedBy");
            ModelState.Remove("Campus");
            
            // Remove SelectedStudentIds from ModelState validation
            var keysToRemove = ModelState.Keys
                .Where(k => k.Contains(".SelectedStudentIds"))
                .ToList();
            
            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            }
            
            var currentUser = await _userManager.GetUserAsync(User);
            var campusId = currentUser?.CampusId;

            if (ModelState.IsValid)
            {
                try
                {
                    var existingClassFee = await _context.ClassFees.FindAsync(id);

                    if (existingClassFee == null)
                    {
                        return NotFound();
                    }

                    int finalCampusId;
                    if (campusId == null || campusId == 0)
                    {
                        var selectedClass = await _context.Classes
                            .FirstOrDefaultAsync(c => c.Id == classFee.ClassId);

                        if (selectedClass == null)
                        {
                            ModelState.AddModelError("ClassId", "Invalid class selected.");
                            return View(classFee);
                        }

                        finalCampusId = selectedClass.CampusId;
                    }
                    else
                    {
                        finalCampusId = campusId.Value;
                    }

                    var duplicateFee = await _context.ClassFees
                        .FirstOrDefaultAsync(cf => cf.ClassId == classFee.ClassId && cf.CampusId == finalCampusId && cf.Id != id);

                    if (duplicateFee != null)
                    {
                        ModelState.AddModelError("ClassId", "Fee structure already exists for this class.");

                        if (campusId == null || campusId == 0)
                        {
                            ViewBag.Classes = _context.Classes
                                .Include(c => c.Campus)
                                .Where(c => c.IsActive)
                                .ToList();
                        }
                        else
                        {
                            ViewBag.Classes = _context.Classes
                                .Include(c => c.Campus)
                                .Where(c => c.IsActive && c.CampusId == campusId)
                                .ToList();
                        }

                        return View(classFee);
                    }

                    // Update fields
                    existingClassFee.ClassId = classFee.ClassId;
                    existingClassFee.TuitionFee = classFee.TuitionFee;
                    existingClassFee.AdmissionFee = classFee.AdmissionFee;
                    existingClassFee.CampusId = finalCampusId;
                    existingClassFee.ModifiedBy = User.Identity?.Name;
                    existingClassFee.ModifiedDate = DateTime.Now;

                    _context.Update(existingClassFee);
                    await _context.SaveChangesAsync();
                    
                    // Soft delete removed charges (set IsDeleted = true and IsActive = false)
                    if (DeletedChargeIds != null && DeletedChargeIds.Any())
                    {
                        var chargesToDelete = await _context.ClassFeeExtraCharges
                            .Where(ec => DeletedChargeIds.Contains(ec.Id) && 
                                        ec.ClassId == classFee.ClassId &&
                                        ec.CampusId == finalCampusId)
                            .ToListAsync();
                        
                        foreach (var charge in chargesToDelete)
                        {
                            charge.IsDeleted = true;
                            charge.IsActive = false;
                            charge.ModifiedBy = User.Identity?.Name;
                            charge.ModifiedDate = DateTime.Now;
                        }
                        
                        _context.UpdateRange(chargesToDelete);
                        await _context.SaveChangesAsync();
                    }
                    
                    // Get all students in the class for default selection
                    var allStudentsInClass = await _context.Students
                        .Where(s => s.Class == classFee.ClassId && !s.HasLeft)
                        .Select(s => s.Id)
                        .ToListAsync();
                    
                    // Update/Add extra charges
                    if (ExtraCharges != null && ExtraCharges.Any())
                    {
                        foreach (var charge in ExtraCharges)
                        {
                            // If no students are selected, default to all students in the class
                            if (charge.SelectedStudentIds == null || !charge.SelectedStudentIds.Any())
                            {
                                charge.SelectedStudentIds = allStudentsInClass;
                            }
                            
                            if (charge.Id > 0)
                            {
                                // Update existing charge
                                var existingCharge = await _context.ClassFeeExtraCharges
                                    .FirstOrDefaultAsync(ec => ec.Id == charge.Id);
                                
                                if (existingCharge != null)
                                {
                                    existingCharge.ChargeName = charge.ChargeName;
                                    existingCharge.Amount = charge.Amount;
                                    existingCharge.Category = charge.Category;
                                    existingCharge.IsActive = charge.IsActive;
                                    existingCharge.ModifiedBy = User.Identity?.Name;
                                    existingCharge.ModifiedDate = DateTime.Now;
                                    
                                    _context.Update(existingCharge);
                                    await _context.SaveChangesAsync();
                                    
                                    // Update student assignments - remove old and add new
                                    var existingAssignments = await _context.StudentChargeAssignments
                                        .Where(sca => sca.ClassFeeExtraChargeId == existingCharge.Id)
                                        .ToListAsync();
                                    
                                    _context.StudentChargeAssignments.RemoveRange(existingAssignments);
                                    await _context.SaveChangesAsync();
                                    
                                    // Add new assignments
                                    if (charge.SelectedStudentIds != null && charge.SelectedStudentIds.Any())
                                    {
                                        foreach (var studentId in charge.SelectedStudentIds)
                                        {
                                            var assignment = new StudentChargeAssignment
                                            {
                                                StudentId = studentId,
                                                ClassFeeExtraChargeId = existingCharge.Id,
                                                IsAssigned = true,
                                                AssignedDate = DateTime.Now,
                                                AssignedBy = User.Identity?.Name,
                                                CampusId = finalCampusId
                                            };
                                            _context.StudentChargeAssignments.Add(assignment);
                                        }
                                        await _context.SaveChangesAsync();
                                    }
                                }
                            }
                            else
                            {
                                // Add new charge
                                var newCharges = new List<ClassFeeExtraChargeViewModel> { charge };
                                await SaveExtraCharges(classFee.ClassId, finalCampusId, newCharges);
                            }
                        }
                    }
                    
                    // Apply to other classes if requested
                    if (applyOption == "all" || (applyOption == "selected" && SelectedClassIds != null && SelectedClassIds.Any()))
                    {
                        var classIdsToApply = applyOption == "all" 
                            ? await _context.Classes.Where(c => c.CampusId == finalCampusId && c.Id != classFee.ClassId).Select(c => c.Id).ToListAsync()
                            : SelectedClassIds.Where(cid => cid != classFee.ClassId).ToList();
                        
                        foreach (var classId in classIdsToApply)
                        {
                            await ApplyFeeStructureToClass(existingClassFee, classId, finalCampusId, ExtraCharges);
                        }
                    }
                    
                    TempData["Success"] = "Fee structure updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClassFeeExists(classFee.Id))
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

            // Repopulate dropdowns on validation fail
            if (campusId == null || campusId == 0)
            {
                ViewBag.Classes = _context.Classes
                    .Include(c => c.Campus)
                    .Where(c => c.IsActive)
                    .ToList();
            }
            else
            {
                ViewBag.Classes = _context.Classes
                    .Include(c => c.Campus)
                    .Where(c => c.IsActive && c.CampusId == campusId)
                    .ToList();
            }

            return View(classFee);
        }



        // GET: ClassFee/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var classFee = await _context.ClassFees
                .Include(cf => cf.Class)
                .ThenInclude(c => c.Campus)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (classFee == null)
            {
                return NotFound();
            }

            return View(classFee);
        }

        // POST: ClassFee/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var classFee = await _context.ClassFees.FindAsync(id);
            _context.ClassFees.Remove(classFee);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ClassFeeExists(int id)
        {
            return _context.ClassFees.Any(e => e.Id == id);
        }

        // Helper method to save extra charges
        private async Task SaveExtraCharges(int classId, int campusId, List<ClassFeeExtraChargeViewModel> extraCharges)
        {
            foreach (var charge in extraCharges)
            {
                var extraCharge = new ClassFeeExtraCharges
                {
                    ChargeName = charge.ChargeName,
                    Amount = charge.Amount,
                    Category = charge.Category,
                    IsActive = charge.IsActive,
                    IsDeleted = false,
                    ClassId = classId,
                    CampusId = campusId,
                    CreatedBy = User.Identity?.Name,
                    ModifiedBy = User.Identity?.Name,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };
                
                _context.ClassFeeExtraCharges.Add(extraCharge);
                await _context.SaveChangesAsync();
                
                // Save student assignments (if SelectedStudentIds is provided)
                if (charge.SelectedStudentIds != null && charge.SelectedStudentIds.Any())
                {
                    foreach (var studentId in charge.SelectedStudentIds)
                    {
                        var assignment = new StudentChargeAssignment
                        {
                            StudentId = studentId,
                            ClassFeeExtraChargeId = extraCharge.Id,
                            IsAssigned = true,
                            AssignedDate = DateTime.Now,
                            AssignedBy = User.Identity?.Name,
                            CampusId = campusId
                        };
                        _context.StudentChargeAssignments.Add(assignment);
                    }
                    await _context.SaveChangesAsync();
                }
            }
        }

        // Helper method to apply fee structure to another class
        private async Task ApplyFeeStructureToClass(ClassFee sourceClassFee, int targetClassId, int campusId, List<ClassFeeExtraChargeViewModel> extraCharges)
        {
            var existingFee = await _context.ClassFees
                .FirstOrDefaultAsync(cf => cf.ClassId == targetClassId && cf.CampusId == campusId);
            
            if (existingFee != null)
            {
                // Update existing fee
                existingFee.TuitionFee = sourceClassFee.TuitionFee;
                existingFee.AdmissionFee = sourceClassFee.AdmissionFee;
                existingFee.ModifiedBy = User.Identity?.Name;
                existingFee.ModifiedDate = DateTime.Now;
                _context.Update(existingFee);
                
                // Soft delete existing extra charges for this class
                var existingCharges = await _context.ClassFeeExtraCharges
                    .Where(ec => ec.ClassId == targetClassId && ec.CampusId == campusId && !ec.IsDeleted)
                    .ToListAsync();
                
                foreach (var charge in existingCharges)
                {
                    charge.IsDeleted = true;
                    charge.IsActive = false;
                    charge.ModifiedBy = User.Identity?.Name;
                    charge.ModifiedDate = DateTime.Now;
                }
                
                _context.UpdateRange(existingCharges);
            }
            else
            {
                // Create new fee
                var newFee = new ClassFee
                {
                    ClassId = targetClassId,
                    TuitionFee = sourceClassFee.TuitionFee,
                    AdmissionFee = sourceClassFee.AdmissionFee,
                    CampusId = campusId,
                    CreatedBy = User.Identity?.Name,
                    ModifiedBy = User.Identity?.Name,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };
                _context.Add(newFee);
            }
            
            await _context.SaveChangesAsync();
            
            // Apply extra charges
            if (extraCharges != null && extraCharges.Any())
            {
                // Get all students in the target class for default selection
                var allStudentsInClass = await _context.Students
                    .Where(s => s.Class == targetClassId && !s.HasLeft)
                    .Select(s => s.Id)
                    .ToListAsync();
                
                foreach (var charge in extraCharges)
                {
                    // Default to all students in the target class
                    charge.SelectedStudentIds = allStudentsInClass;
                }
                
                await SaveExtraCharges(targetClassId, campusId, extraCharges);
            }
        }

        // API endpoint to get fee by class ID
        [HttpGet]
        public async Task<IActionResult> GetFeeByClassId(int classId)
        {
            var classFee = await _context.ClassFees
                .FirstOrDefaultAsync(cf => cf.ClassId == classId);

            if (classFee == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                TuitionFee = classFee.TuitionFee,
                AdmissionFee = classFee.AdmissionFee,
                TotalFee = classFee.TuitionFee + classFee.AdmissionFee
            });
        }

        // API endpoint to get extra charges for a class fee
        [HttpGet]
        public async Task<IActionResult> GetExtraCharges(int classFeeId)
        {
            var classFee = await _context.ClassFees.FindAsync(classFeeId);
            if (classFee == null)
            {
                return NotFound();
            }

            var charges = await _context.ClassFeeExtraCharges
                .Where(ec => ec.ClassId == classFee.ClassId &&  !ec.IsDeleted)
                .Include(ec => ec.ExcludedStudents)
                .Select(ec => new
                {
                    id = ec.Id,
                    chargeName = ec.ChargeName,
                    amount = ec.Amount,
                    category = ec.Category,
                    isActive = ec.IsActive,
                    excludedStudentIds = ec.ExcludedStudents.Select(e => e.StudentId).ToList()
                })
                .ToListAsync();

            return Ok(charges);
        }

        // API endpoint to get students by class with section info
        [HttpGet]
        public async Task<IActionResult> GetStudentsByClass(int classId)
        {
            var students = await _context.Students
                .Where(s => s.Class == classId && !s.HasLeft)
                .Select(s => new
                {
                    id = s.Id,
                    name = s.StudentName,
                    sectionId = s.Section,
                    sectionName = s.SectionObj.Name,
                    cnic = s.StudentCNIC,
                    phone = s.PhoneNumber
                })
                .OrderBy(s => s.sectionName)
                .ThenBy(s => s.name)
                .ToListAsync();

            return Ok(students);
        }

        // API endpoint to get optional charges for a class
        [HttpGet]
        public async Task<IActionResult> GetOptionalChargesByClass(int classId, int? studentId = null)
        {
            var charges = await _context.ClassFeeExtraCharges
                .Where(ec => ec.ClassId == classId  && !ec.IsDeleted)
                .Select(ec => new
                {
                    id = ec.Id,
                    chargeName = ec.ChargeName,
                    amount = ec.Amount,
                    category = ec.Category,
                    isAssigned = studentId.HasValue 
                        ? _context.StudentChargeAssignments.Any(sca => sca.StudentId == studentId.Value && sca.ClassFeeExtraChargeId == ec.Id && sca.IsAssigned)
                        : true
                })
                .ToListAsync();

            return Ok(charges);
        }

        // API endpoint to get extra charges with student assignments for edit view
        [HttpGet]
        public async Task<IActionResult> GetExtraChargesWithAssignments(int classFeeId)
        {
            var classFee = await _context.ClassFees.FindAsync(classFeeId);
            if (classFee == null)
            {
                return NotFound();
            }

            var charges = await _context.ClassFeeExtraCharges
                .Where(ec => ec.ClassId == classFee.ClassId && !ec.IsDeleted)
                .Select(ec => new
                {
                    id = ec.Id,
                    chargeName = ec.ChargeName,
                    amount = ec.Amount,
                    category = ec.Category,
                    isActive = ec.IsActive,
                    selectedStudentIds = _context.StudentChargeAssignments
                        .Where(sca => sca.ClassFeeExtraChargeId == ec.Id && sca.IsAssigned)
                        .Select(sca => sca.StudentId)
                        .ToList(),
                    assignedStudentCount = _context.StudentChargeAssignments
                        .Count(sca => sca.ClassFeeExtraChargeId == ec.Id && sca.IsAssigned),
                    totalStudents = _context.Students
                        .Count(s => s.Class == classFee.ClassId && !s.HasLeft)
                })
                .ToListAsync();

            return Ok(charges);
        }

        // API endpoint to get assigned students for a charge
        [HttpGet]
        public async Task<IActionResult> GetAssignedStudents(int chargeId, int classId)
        {
            var assignedStudents = await _context.StudentChargeAssignments
                .Where(sca => sca.ClassFeeExtraChargeId == chargeId && sca.IsAssigned)
                .Include(sca => sca.Student)
                    .ThenInclude(s => s.SectionObj)
                .Where(sca => sca.Student.Class == classId && !sca.Student.HasLeft)
                .Select(sca => new
                {
                    studentId = sca.StudentId,
                    studentName = sca.Student.StudentName,
                    sectionId = sca.Student.Section,
                    sectionName = sca.Student.SectionObj.Name
                })
                .ToListAsync();

            // Group by section
            var groupedBySection = assignedStudents
                .GroupBy(s => new { s.sectionId, s.sectionName })
                .Select(g => new
                {
                    sectionId = g.Key.sectionId,
                    sectionName = g.Key.sectionName,
                    students = g.Select(s => new
                    {
                        id = s.studentId,
                        name = s.studentName
                    }).ToList()
                })
                .OrderBy(g => g.sectionName)
                .ToList();

            return Ok(new { sections = groupedBySection });
        }
    }
}