using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using UTS_SMS.Services;
using UTS_SMS.ViewModels;

namespace UTS_SMS.Controllers
{
    public class BillingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IExtraChargeService _extraChargeService;
        private readonly NotificationService _notificationService;

        public BillingController(ApplicationDbContext context, IUserService userService, IWebHostEnvironment env, UserManager<ApplicationUser> userManager, IExtraChargeService extraChargeService, NotificationService notificationService)
        {
            _context = context;
            _userService = userService;
            _env = env;
            _userManager = userManager;
            _extraChargeService = extraChargeService;
            _notificationService = notificationService;
        }

        // GET: Students
        public async Task<IActionResult> Index(
      string sortOrder,
      string currentFilter,
      string searchString,
      string classFilter,
      string sectionFilter,
      int? campusFilter,
      bool? showLeft,
      int? forMonth,
      int? forYear)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser.CampusId;

            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["ClassSortParm"] = sortOrder == "Class" ? "class_desc" : "Class";

            // Campus dropdown
            var campusesQuery = _context.Campuses.AsQueryable();

            // If user has a specific campus, only show that campus
            if (userCampusId.HasValue && userCampusId.Value > 0)
            {
                campusesQuery = campusesQuery.Where(c => c.Id == userCampusId.Value);
            }

            ViewBag.Campuses = await campusesQuery
                .OrderBy(c => c.Name)
                .ToListAsync();

            // Class dropdown - filter by campus if selected
            var classesQuery = _context.Classes.AsQueryable();
            if (campusFilter.HasValue && campusFilter.Value > 0)
            {
                classesQuery = classesQuery.Where(c => c.CampusId == campusFilter.Value);
                ViewBag.Classes = await classesQuery
                .Select(c => c.Name)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
            }

            

            // Section dropdown - filter by class if selected
            var sectionsQuery = _context.ClassSections.AsQueryable();
            if (!string.IsNullOrEmpty(classFilter))
            {
                var classId = await _context.Classes
                    .Where(c => c.Name == classFilter)
                    .Select(c => c.Id)
                    .FirstOrDefaultAsync();

                if (classId > 0)
                {
                    sectionsQuery = sectionsQuery.Where(s => s.ClassId == classId);
                    ViewBag.Sections = await sectionsQuery
              .Select(s => s.Name)
              .Distinct()
              .OrderBy(s => s)
              .ToListAsync();
                }
            }

          

            ViewBag.AvailableYears = await _context.BillingMaster
                .Select(b => b.ForYear)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            if (searchString != null)
                currentFilter = searchString;

            var selectedMonth = forMonth ?? DateTime.Now.Month;
            var selectedYear = forYear ?? DateTime.Now.Year;
            
            // Create the last day of the selected month for comparison
            var selectedMonthEndDate = new DateTime(selectedYear, selectedMonth, 1).AddMonths(1).AddDays(-1);

            ViewData["CurrentFilter"] = searchString;
            ViewData["CampusFilter"] = campusFilter;
            ViewData["ClassFilter"] = classFilter;
            ViewData["SectionFilter"] = sectionFilter;
            ViewData["ShowLeft"] = showLeft;
            ViewData["ForMonth"] = selectedMonth;
            ViewData["ForYear"] = selectedYear;

            var studentsQuery = _context.Students
                .Where(s => s.HasLeft == false && s.RegistrationDate <= selectedMonthEndDate)
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .Include(s => s.Campus)
                .AsQueryable();

            // Apply campus filter
            if (campusFilter.HasValue && campusFilter.Value > 0)
            {
                studentsQuery = studentsQuery.Where(s => s.CampusId == campusFilter.Value);
            }
            else if (userCampusId.HasValue && userCampusId.Value > 0)
            {
                // If user has a specific campus but no filter selected, show only that campus
                studentsQuery = studentsQuery.Where(s => s.CampusId == userCampusId.Value);
            }

            var studentsWithBilling = studentsQuery.Select(s => new
            {
                Student = s,
                SelectedBilling = _context.BillingMaster
                    .Where(b => b.StudentId == s.Id &&
                                b.ForMonth == selectedMonth &&
                                b.ForYear == selectedYear)
                    .FirstOrDefault(),
                HasFutureBilling = _context.BillingMaster
                    .Any(b => b.StudentId == s.Id &&
                             (b.ForYear > selectedYear ||
                             (b.ForYear == selectedYear && b.ForMonth > selectedMonth)))
            });

            // Apply other filters
            if (showLeft.HasValue)
                studentsWithBilling = studentsWithBilling.Where(x => x.Student.HasLeft == showLeft);

            if (!string.IsNullOrEmpty(searchString))
            {
                studentsWithBilling = studentsWithBilling.Where(x =>
                    x.Student.StudentName.Contains(searchString) ||
                    x.Student.FatherName.Contains(searchString) ||
                    x.Student.StudentCNIC.Contains(searchString) ||
                    x.Student.FatherCNIC.Contains(searchString) ||
                    x.Student.PhoneNumber.Contains(searchString) ||
                    x.Student.FatherPhone.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(classFilter))
                studentsWithBilling = studentsWithBilling.Where(x => x.Student.ClassObj.Name == classFilter);

            if (!string.IsNullOrEmpty(sectionFilter))
                studentsWithBilling = studentsWithBilling.Where(x => x.Student.SectionObj.Name == sectionFilter);

            // Sorting
            switch (sortOrder)
            {
                case "name_desc":
                    studentsWithBilling = studentsWithBilling.OrderByDescending(x => x.Student.StudentName);
                    break;
                case "Date":
                    studentsWithBilling = studentsWithBilling.OrderBy(x => x.Student.RegistrationDate);
                    break;
                case "date_desc":
                    studentsWithBilling = studentsWithBilling.OrderByDescending(x => x.Student.RegistrationDate);
                    break;
                case "Class":
                    studentsWithBilling = studentsWithBilling.OrderBy(x => x.Student.ClassObj.Name);
                    break;
                case "class_desc":
                    studentsWithBilling = studentsWithBilling.OrderByDescending(x => x.Student.ClassObj.Name);
                    break;
                default:
                    studentsWithBilling = studentsWithBilling.OrderBy(x => x.Student.StudentName);
                    break;
            }

            var viewModelQuery = studentsWithBilling.Select(x => new StudentBillingViewModel
            {
                Student = x.Student,
                LatestBilling = x.SelectedBilling,
                Dues = x.SelectedBilling != null ? x.SelectedBilling.Dues : -1,
                RowColor = x.HasFutureBilling ? "bg-green-100" :
                          x.SelectedBilling == null ? "bg-red-100" :
                          x.SelectedBilling.Dues == 0 ? "bg-green-100" :
                          "bg-yellow-100",
                CanPay = !x.HasFutureBilling &&
                        (x.SelectedBilling == null || x.SelectedBilling.Dues > 0)
            });

            return View(await viewModelQuery.AsNoTracking().ToListAsync());
        }


        [HttpGet]
        public async Task<IActionResult> Create(int id, int? forMonth, int? forYear)
        {
            // Load student with class information
            var student = await _context.Students
                .Include(s => s.ClassObj)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null) return NotFound();
            if (student.ClassObj == null) return BadRequest("Student class information not found.");

            // Validate month and year
            var selectedMonth = forMonth ?? DateTime.Now.Month;
            var selectedYear = forYear ?? DateTime.Now.Year;

            if (selectedMonth < 1 || selectedMonth > 12) selectedMonth = DateTime.Now.Month;
            if (selectedYear < 2000 || selectedYear > DateTime.Now.Year + 1) selectedYear = DateTime.Now.Year;

            var classFee = await _context.ClassFees
                .FirstOrDefaultAsync(cf => cf.ClassId == student.ClassObj.Id);

            if (classFee == null) return BadRequest("Class fee structure not found.");
            if (classFee.TuitionFee == 0) return BadRequest("Tuition fee not set for this class.");

            // Calculate tuition fee with discount
            var tuitionFee = classFee.TuitionFee * (1 - ((student.TuitionFeeDiscountPercent ?? 0) / 100m));
            decimal? miscallaneous = 0m; // TODO: Calculate optional charges sum later

            // Calculate fine based on date comparison
            var currentDate = DateTime.Now;
            var billingDate = new DateTime(selectedYear, selectedMonth, 1);
            decimal fine = 0;

            

            // Check for existing billing with its transactions
            var existingBilling = await _context.BillingMaster
                .Include(b => b.Transactions)
                .Where(b => b.StudentId == id &&
                           b.ForMonth == selectedMonth &&
                           b.ForYear == selectedYear)
                .FirstOrDefaultAsync();
         
            decimal alreadyPaidAmount = 0;
            if (existingBilling != null)
            {
                alreadyPaidAmount = existingBilling.Transactions?.Sum(t => t.AmountPaid) ?? 0;
                TempData["ErrorMessage"] = $"Billing for {selectedMonth}/{selectedYear} already exists. Already Paid: ₨{alreadyPaidAmount:N0}";
                if (existingBilling.Dues == 0)
                {
                    return BadRequest($"Already Paid Full for {selectedMonth}/{selectedYear}");
                }
            }

            decimal extraChargesAmount = 0m;
            var extraChargeItems = new List<ExtraChargeItem>();

            // ✅ Always calculate extra charges and fines (for both new and existing bills)
            // Calculate extra charges
            extraChargesAmount = await _extraChargeService.CalculateExtraCharges(student.Class, id, student.CampusId);
            var applicableCharges = await _extraChargeService.GetApplicableCharges(student.Class, id, student.CampusId);


            foreach (var charge in applicableCharges)
            {
                bool shouldInclude = false;
                switch (charge.Category)
                {
                    case "MonthlyCharges":
                        shouldInclude = true;
                        break;
                    case "OncePerLifetime":
                        shouldInclude = !await _extraChargeService.HasPaidCharge(id, charge.Id);
                        break;
                    case "OncePerClass":
                        shouldInclude = !await _extraChargeService.HasPaidCharge(id, charge.Id, student.Class);
                        break;
                }

                if (shouldInclude)
                {
                    extraChargeItems.Add(new ExtraChargeItem
                    {
                        ChargeId = charge.Id,
                        ChargeName = charge.ChargeName,
                        Amount = charge.Amount,
                        Category = charge.Category
                    });
                }
            }

            

            // ✅ ADD UNPAID FINES TO EXTRA CHARGE ITEMS
            var unpaidFines = await _context.StudentFineCharges
                .Where(sfc => sfc.StudentId == id && !sfc.IsPaid && sfc.IsActive)
                .ToListAsync();

            foreach (var finee in unpaidFines)
            {
                extraChargeItems.Add(new ExtraChargeItem
                {
                    ChargeId = finee.Id,
                    ChargeName = finee.ChargeName,
                    Amount = finee.Amount,
                    Category = "Fine/Charge"
                });
                extraChargesAmount += finee.Amount;
            }

            // Only use 0 for extra charges if this is an existing bill with no new charges
            if (existingBilling != null && extraChargeItems.Count == 0)
            {
                extraChargesAmount = 0m;
            }

            // --- Step 1: Calculate Current Month Components ---
            var currentTuition = existingBilling != null ? existingBilling.TuitionFee : tuitionFee;
            var currentAdmission = existingBilling != null ? existingBilling.AdmissionFee : 
                (student.AdmissionFeePaid ? 0 :
                Math.Max(0, classFee.AdmissionFee - (classFee.AdmissionFee * ((student.AdmissionFeeDiscountAmount ?? 0) / 100m))));
            var currentFine = existingBilling != null ? existingBilling.Fine : fine;
            var currentMisc = existingBilling != null ? existingBilling.MiscallaneousCharges : extraChargesAmount;
            decimal calculatedPreviousDues = 0;
            string duesRemarks = "No previous dues record found";

            // --- Step 2: Fetch and Calculate Dues Logic (Improved) ---
            // Get the last billing record before the current month
            var lastRecord = await _context.BillingMaster
                .Include(b => b.Transactions)
                .Where(b => b.StudentId == id && 
                           (b.ForYear < selectedYear || (b.ForYear == selectedYear && b.ForMonth < selectedMonth)))
                .OrderByDescending(b => b.ForYear)
                .ThenByDescending(b => b.ForMonth)
                .FirstOrDefaultAsync();

            if (lastRecord != null)
            {
                // Calculate Previous Dues = TotalPayable from last bill - Sum of all transactions for that bill
                var lastBillTotalPayable = lastRecord.TuitionFee + lastRecord.AdmissionFee + 
                                           lastRecord.MiscallaneousCharges + lastRecord.PreviousDues + lastRecord.Fine;
                var lastBillTotalPaid = lastRecord.Transactions?.Sum(t => t.AmountPaid) ?? 0;
                calculatedPreviousDues = lastBillTotalPayable - lastBillTotalPaid;
                
                var lastRecordDate = new DateTime(lastRecord.ForYear, lastRecord.ForMonth, 1);
                var selectedDate = new DateTime(selectedYear, selectedMonth, 1);

                var monthsGap = ((selectedDate.Year - lastRecordDate.Year) * 12)
                                + selectedDate.Month - lastRecordDate.Month - 1;

                var lastMonthName = System.Globalization.DateTimeFormatInfo.CurrentInfo.GetMonthName(lastRecord.ForMonth);

                if (monthsGap > 0)
                {
                    // Add missing months' fees (tuition + extra charges for each missing month)
                    var missingMonthsAmount = monthsGap * (tuitionFee + extraChargesAmount);
                    calculatedPreviousDues += missingMonthsAmount;

                    duesRemarks = $"Previous dues from {lastMonthName} {lastRecord.ForYear}: ₨{(lastBillTotalPayable - lastBillTotalPaid):N0}. " +
                                  $"Plus {monthsGap} month(s) missing fees: ₨{missingMonthsAmount:N0}.";
                }
                else
                {
                    duesRemarks = $"Previous dues from {lastMonthName} {lastRecord.ForYear}: ₨{calculatedPreviousDues:N0}";
                }
            }
            //else
            //{
            //    // No billing record exists - check if student should have dues from registration date
            //    var registrationMonth = student.RegistrationDate.Month;
            //    var registrationYear = student.RegistrationDate.Year;
            //    var monthsSinceRegistration = ((selectedYear - registrationYear) * 12) + selectedMonth - registrationMonth;
                
            //    if (monthsSinceRegistration > 0)
            //    {
            //        // Student has been registered for more than one month without any billing
            //        calculatedPreviousDues = monthsSinceRegistration * (tuitionFee + extraChargesAmount);
            //        duesRemarks = $"No billing history found. {monthsSinceRegistration} month(s) since registration: ₨{calculatedPreviousDues:N0}.";
            //    }
            //}
            
            // If existing billing, use its previous dues value
            if (existingBilling != null)
            {
                calculatedPreviousDues = existingBilling.PreviousDues;
                duesRemarks = existingBilling.RemarksPreviousDues ?? "From existing billing record";
            }

            // --- Step 3: Create the ViewModel ---
            var billingViewModel = new BillingCreateViewModel
            {
                StudentId = id,
                StudentName = student?.StudentName ?? "Unknown", // Null-safety check
                ForMonth = selectedMonth,
                ForYear = selectedYear,
                TuitionFee = currentTuition,
                AdmissionFee = currentAdmission,
                Fine = currentFine,
                PaymentDate = DateTime.Now,
                MiscallaneousCharges = currentMisc,
                ExtraCharges = currentMisc,
                ExtraChargeItems = existingBilling != null ? new List<ExtraChargeItem>() : extraChargeItems,
                PreviousDues = calculatedPreviousDues,
                RemarksPreviousDues = duesRemarks,

                // Default Total: includes current items + previous dues
                TotalPayable = currentTuition + currentAdmission + currentFine + currentMisc + calculatedPreviousDues
            };
            
            // Set AlreadyPaid for existing billing records
            ViewBag.AlreadyPaid = alreadyPaidAmount;
            ViewBag.IsExistingBilling = existingBilling != null;

            // Check for Employee Parent category with salary deduction
            // Check for Employee Parent category with salary deduction
            var categoryAssignment = await _context.StudentCategoryAssignments
                .Include(sca => sca.StudentCategory)
                .Include(sca => sca.Employee)
                .Where(sca => sca.StudentId == id && sca.IsActive)
                .FirstOrDefaultAsync();

            if (categoryAssignment != null &&
                categoryAssignment.StudentCategory.CategoryType == "EmployeeParent" &&
                (categoryAssignment.PaymentMode == "CutFromSalary" || categoryAssignment.PaymentMode == "CustomRatio"))
            {
                var employeeSalary = await _context.SalaryDefinitions
                    .Where(sd => sd.EmployeeId == categoryAssignment.EmployeeId && sd.IsActive)
                    .FirstOrDefaultAsync();

                if (employeeSalary != null && categoryAssignment.EmployeeId.HasValue)
                {
                    // ---------------------------------------------------------
                    // 1. GET SIBLING DETAILS & CALCULATE USED SALARY
                    // ---------------------------------------------------------

                    // Find IDs of other students linked to this employee
                    var siblingStudentIds = await _context.StudentCategoryAssignments
                        .Where(sca => sca.EmployeeId == categoryAssignment.EmployeeId  && sca.IsActive)
                        .Select(sca => sca.StudentId)
                        .ToListAsync();

                    decimal usedSalaryAmount = 0m;
                    var siblingUsageList = new List<string>(); // List to store text details

                    if (siblingStudentIds.Any())
                    {
                        // 1. Fetch Sibling Bills (Masters)
                        // We use .Select() to avoid the "Include" error and get exactly what we need
                        var siblingBillsData = await _context.BillingMaster
                            .Where(b => siblingStudentIds.Contains(b.StudentId)
                                     && b.ForMonth == selectedMonth
                                     && b.ForYear == selectedYear)
                            .Select(b => new
                            {
                                MasterId = b.Id,
                                Name = b.Student.StudentName,
                                ClassName = "", // Access Class directly from Bill

                                // GROSS TOTAL: The full amount before any deductions or payments
                                GrossTotal = b.TuitionFee + b.AdmissionFee + b.Fine + b.MiscallaneousCharges + b.PreviousDues
                            })
                            .ToListAsync();

                        // 2. Fetch Payments (Transactions) for these bills
                        // We do this in a second query to be efficient and avoid complex group-join issues
                        var billIds = siblingBillsData.Select(b => b.MasterId).ToList();

                        var payments = await _context.SalaryDeductions
                            .Where(t => billIds.Contains(t.BillingMasterId))
                            .GroupBy(t => t.BillingMasterId)
                            .Select(g => new
                            {
                                MasterId = g.Key,
                                TotalPaid = g.Sum(x => x.AmountDeducted)
                            })
                            .ToListAsync();

                        // 3. Calculate "Used Salary" based on the gap
                        // Logic: Gross Fee - Amount Paid by Parent = Amount Covered by Salary
                        foreach (var bill in siblingBillsData)
                        {
                            // Find the payment sum for this bill (or 0 if no payments yet)
                            var paidAmount = payments.FirstOrDefault(p => p.MasterId == bill.MasterId)?.TotalPaid ?? 0m;
 
 
                            if (paidAmount > 0)
                            {
                                usedSalaryAmount += paidAmount;
                                siblingUsageList.Add($"{bill.Name} : Rs. {paidAmount:N0}");
                            }
                        }
                    }

                    var availableSalary = employeeSalary.NetSalary - usedSalaryAmount;

                    // ---------------------------------------------------------
                    // 2. CALCULATE CURRENT BILL & APPLY CAP
                    // ---------------------------------------------------------
                    var currentBillAmount = billingViewModel.TuitionFee +
                                            billingViewModel.AdmissionFee +
                                            billingViewModel.MiscallaneousCharges;

                    var deductionPercent = categoryAssignment.CustomTuitionPercent ?? 100m;
                    var calculatedDeduction = (currentBillAmount * deductionPercent) / 100m;

                    decimal finalDeduction = 0;
                    string warningMessage = "";

                    // ... (Same cap logic as before) ...
                    if (availableSalary <= 0)
                    {
                        finalDeduction = 0;
                        warningMessage = " (Salary limit reached by siblings)";
                    }
                    else if (calculatedDeduction > availableSalary)
                    {
                        finalDeduction = availableSalary;
                        warningMessage = $" (Salary cap reached)";
                    }
                    else
                    {
                        finalDeduction = calculatedDeduction;
                    }

                    // ---------------------------------------------------------
                    // 3. FINAL SETUP
                    // ---------------------------------------------------------
                    var payableByCash = (currentBillAmount - finalDeduction) + billingViewModel.PreviousDues;
                    billingViewModel.TotalPayable = payableByCash;
                    billingViewModel.SalaryDeductionAmount = finalDeduction;

                    ViewBag.IsEmployeeParent = true;
                    ViewBag.EmployeeName = categoryAssignment.Employee?.FullName ?? "N/A";
                    ViewBag.TotalFeeAmount = currentBillAmount;
                    ViewBag.DeductionAmount = finalDeduction;
                    ViewBag.PayableByCash = payableByCash;
                    ViewBag.EmployeeSalary = employeeSalary.NetSalary;
                    ViewBag.UsedSalary = usedSalaryAmount;
                    ViewBag.AvailableSalary = availableSalary;

                    // Pass the list of sibling details to the View
                    ViewBag.SiblingDetails = siblingUsageList;

                    // Note for the bottom of the bill
                    var siblingNote = siblingUsageList.Any() ? $" [Siblings: {string.Join(", ", siblingUsageList)}]" : "";

                    ViewBag.FeeCalculationNote = $"Bill: {currentBillAmount:N0}. " +
                                                 $"Siblings Used: {usedSalaryAmount:N0}{siblingNote}. " +
                                                 $"Deducted: {finalDeduction:N0} {warningMessage}.";
                }
            }


            ViewData["AccountId"] = new SelectList(
     _context.BankAccounts
         .Where(cs => cs.IsActive)
         .Select(cs => new
         {
             cs.Id,
             DisplayName = cs.BankName + " - " + cs.AccountTitle + "(" + cs.AccountNumber + ")"
         })
         .ToList(),
     "Id",
     "DisplayName"
 );

            return View(billingViewModel);
        }
        [HttpGet]
        public async Task<JsonResult> GetClassesByCampus(int? campusId)
        {
            var classesQuery = _context.Classes.AsQueryable();

            if (campusId.HasValue && campusId.Value > 0)
            {
                classesQuery = classesQuery.Where(c => c.CampusId == campusId.Value);
            }

            var classes = await classesQuery
                .Select(c => c.Name)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Json(classes);
        }

        [HttpGet]
        public async Task<JsonResult> GetSectionsByClass(string className)
        {
            var sectionsQuery = _context.ClassSections.AsQueryable();

            if (!string.IsNullOrEmpty(className))
            {
                var classId = await _context.Classes
                    .Where(c => c.Name == className)
                    .Select(c => c.Id)
                    .FirstOrDefaultAsync();

                if (classId > 0)
                {
                    sectionsQuery = sectionsQuery.Where(s => s.ClassId == classId);
                }
            }

            var sections = await sectionsQuery
                .Select(s => s.Name)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            return Json(sections);
        }

        // Get unpaid fines for a student
        [HttpGet]
        public async Task<JsonResult> GetUnpaidFines(int studentId)
        {
            var unpaidFines = await _context.StudentFineCharges
                .Where(sfc => sfc.StudentId == studentId && !sfc.IsPaid && sfc.IsActive)
                .Select(sfc => new
                {
                    id = sfc.Id,
                    chargeName = sfc.ChargeName,
                    amount = sfc.Amount,
                    chargeDate = sfc.ChargeDate.ToString("dd MMM yyyy"),
                    description = sfc.Description
                })
                .ToListAsync();

            return Json(unpaidFines);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BillingCreateViewModel viewModel)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            ModelState.Remove(nameof(BillingCreateViewModel.StudentName));
            ModelState.Remove(nameof(BillingCreateViewModel.ForMonth));
            ModelState.Remove(nameof(BillingCreateViewModel.ForYear));

            var student = await _context.Students
                .Include(s => s.ClassObj)
                .FirstOrDefaultAsync(s => s.Id == viewModel.StudentId);
            var campusId = student.CampusId;

            if (student == null)
            {
                ModelState.AddModelError("", "Student not found.");
                return View(viewModel);
            }

            if (student.ClassObj == null)
            {
                ModelState.AddModelError("", "Student class information not found.");
                return View(viewModel);
            }

            var classFee = await _context.ClassFees
                .FirstOrDefaultAsync(cf => cf.ClassId == student.ClassObj.Id);

            if (classFee == null)
            {
                ModelState.AddModelError("", "Class fee structure not found.");
                return View(viewModel);
            }
             if (ModelState.IsValid)
            {
                // Recalculate fees to prevent tampering
                var recalculatedTuitionFee = classFee.TuitionFee * (1 - ((student.TuitionFeeDiscountPercent ?? 0) / 100m));
                var recalculatedAdmissionFee = student.AdmissionFeePaid ? 0 :
                Math.Max(0, classFee.AdmissionFee - (classFee.AdmissionFee * ((student.AdmissionFeeDiscountAmount ?? 0) / 100m)));

                // ✅ FIX: Calculate extra charges from submitted items
                var recalculatedExtraCharges = viewModel.ExtraChargeItems?.Sum(x => x.Amount) ?? 0m;
                var recalculatedMisc = recalculatedExtraCharges;

                // Check for existing billing FIRST
                var existingMaster = await _context.BillingMaster
                    .FirstOrDefaultAsync(b => b.StudentId == viewModel.StudentId &&
                                    b.ForMonth == viewModel.ForMonth &&
                                    b.ForYear == viewModel.ForYear);

                BillingMaster billingMaster;
                bool isNewRecord = existingMaster == null;

                if (existingMaster != null)
                {
                    // ✅ Existing billing - update dues correctly
                    // Get total already paid
                    var totalAlreadyPaid = await _context.BillingTransactions
                        .Where(t => t.BillingMasterId == existingMaster.Id)
                        .SumAsync(t => t.AmountPaid);
            
                    // Calculate original total
                    var originalTotal = existingMaster.TuitionFee + existingMaster.AdmissionFee + 
                               existingMaster.MiscallaneousCharges + existingMaster.Fine + existingMaster.PreviousDues;
            
                    // Add new charges if any
                    existingMaster.MiscallaneousCharges += recalculatedMisc;
                    existingMaster.Fine += viewModel.Fine;
            
                    // New total after adding charges
                    var newTotal = existingMaster.TuitionFee + existingMaster.AdmissionFee + 
                          existingMaster.MiscallaneousCharges + existingMaster.Fine + existingMaster.PreviousDues;
            
                    // Calculate new dues: New Total - (Already Paid + Current Payment)
                    existingMaster.Dues = newTotal - totalAlreadyPaid - viewModel.TotalPaid;
                    existingMaster.ModifiedDate = DateTime.Now;
                    existingMaster.ModifiedBy = User.Identity.Name;

                    _context.Update(existingMaster);
                    billingMaster = existingMaster;
                }
                else
                {
                    // New billing record
                    var totalAmount = recalculatedTuitionFee + recalculatedAdmissionFee + 
                                     recalculatedMisc + viewModel.Fine + viewModel.PreviousDues;
                    var dues = totalAmount - viewModel.TotalPaid;

                    billingMaster = new BillingMaster
                    {
                        StudentId = viewModel.StudentId,
                        ClassId = student.Class,
                        ForMonth = viewModel.ForMonth,
                        ForYear = viewModel.ForYear,
                        TuitionFee = recalculatedTuitionFee,
                        AdmissionFee = recalculatedAdmissionFee,
                        MiscallaneousCharges = recalculatedMisc,
                        PreviousDues = viewModel.PreviousDues,
                        Fine = viewModel.Fine,
                        Dues = dues,
                        CreatedDate = DateTime.Now,
                        CreatedBy = User.Identity.Name,
                        CampusId = (int)campusId,
                        RemarksPreviousDues = viewModel.RemarksPreviousDues ?? ""
                    };

                    _context.Add(billingMaster);
                }

                // Validate payment amounts
                if (viewModel.CashPaid + viewModel.OnlinePaid != viewModel.TotalPaid)
                {
                    ModelState.AddModelError("", "Cash paid + Online paid must equal Total Paid.");
                    viewModel.StudentName = student.StudentName;
                    return View(viewModel);
                }

                // Validate online account is provided when online payment is made
                if (viewModel.OnlinePaid > 0 && (viewModel.OnlineAccount == null || viewModel.OnlineAccount == 0))
                {
                    ModelState.AddModelError("", "Online Account must be selected when Online Paid amount is greater than 0.");
                    viewModel.StudentName = student.StudentName;
                    return View(viewModel);
                }
                
                // Handle admission fee payment
                if (!student.AdmissionFeePaid && recalculatedAdmissionFee > 0)
                {
                    student.AdmissionFeePaid = true;
                    _context.Students.Update(student);
                }

                await _context.SaveChangesAsync(); // Save to get the ID

                if (isNewRecord)
                {
                    // Get the latest billing record before creating missing months
                    var latestBilling = await _context.BillingMaster
                        .Where(b => b.StudentId == viewModel.StudentId && b.Id != billingMaster.Id)
                        .OrderByDescending(b => b.ForYear)
                        .ThenByDescending(b => b.ForMonth)
                        .FirstOrDefaultAsync();

                    if (latestBilling != null)
                    {
                        var currentDate = new DateTime(viewModel.ForYear, viewModel.ForMonth, 1);
                        var lastDate = new DateTime(latestBilling.ForYear, latestBilling.ForMonth, 1);

                        while (lastDate.AddMonths(1) < currentDate)
                        {
                            lastDate = lastDate.AddMonths(1);

                            var missingMonthRecord = new BillingMaster
                            {
                                StudentId = viewModel.StudentId,
                                ClassId = student.Class,
                                ForMonth = lastDate.Month,
                                ForYear = lastDate.Year,
                                TuitionFee = recalculatedTuitionFee,
                                AdmissionFee = recalculatedAdmissionFee,
                                MiscallaneousCharges = recalculatedMisc,
                                PreviousDues = 0,
                                Fine = 0,
                                Dues = recalculatedTuitionFee + recalculatedAdmissionFee + recalculatedMisc,
                                CreatedDate = DateTime.Now,
                                CreatedBy = "System",
                                CampusId = (int)campusId,
                                Remarks = $"Late Fees TRANSFERRED to month {viewModel.ForMonth}/{viewModel.ForYear}",
                                RemarksPreviousDues = "System-generated missing month record"
                            };

                            _context.Add(missingMonthRecord);
                        }
                    }

                    // Update previous billing records
                    if (viewModel.TotalPaid >= viewModel.PreviousDues)
                    {
                        var previousBillings = await _context.BillingMaster
                            .Where(b => b.StudentId == viewModel.StudentId &&
                                       (b.ForYear < viewModel.ForYear ||
                                       (b.ForYear == viewModel.ForYear && b.ForMonth < viewModel.ForMonth)) &&
                                       b.Dues > 0)
                            .ToListAsync();

                        foreach (var previousBilling in previousBillings)
                        {
                            previousBilling.Remarks = $"Pending dues {previousBilling.Dues} TRANSFERRED to {viewModel.ForMonth}/{viewModel.ForYear}";
                            _context.Update(previousBilling);
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                // Create billing transaction only if payment was made
                BillingTransaction billingTransaction = null;

                if (viewModel.TotalPaid > 0)
                {
                    if(viewModel.OnlineAccount == 0)
                    {
                        viewModel.OnlineAccount = null;
                    }
                    billingTransaction = new BillingTransaction
                    {
                        BillingMasterId = billingMaster.Id,
                        AmountPaid = viewModel.TotalPaid,
                        CashPaid = viewModel.CashPaid,
                        OnlinePaid = viewModel.OnlinePaid,
                        OnlineAccount = viewModel.OnlineAccount,
                        TransactionReference = "N/A",
                        PaymentDate = viewModel.PaymentDate,
                        ReceivedBy = User.Identity.Name,
                        CampusId = (int)campusId,
                    };
                    _context.Add(billingTransaction);
                    await _context.SaveChangesAsync();
                    
                    // Save extra charge payment history
                    if (viewModel.ExtraChargeItems != null && viewModel.ExtraChargeItems.Any())
                    {
                        foreach (var chargeItem in viewModel.ExtraChargeItems)
                        {
                            // Only save non-fine charges to payment history
                            if (chargeItem.Category != "Fine/Charge")
                            {
                                await _extraChargeService.SavePaymentHistory(
                                    viewModel.StudentId,
                                    chargeItem.ChargeId,
                                    billingMaster.Id,
                                    student.Class,
                                    chargeItem.Amount,
                                    (int)campusId
                                );
                            }
                        }
                    }

                    // Create notification for fee received
                    await _notificationService.CreateFeeReceivedNotification(
                        viewModel.StudentId,
                        viewModel.TotalPaid,
                        (int)campusId,
                        User.Identity.Name
                    );

                    // Mark unpaid student fines/charges as paid
                    var unpaidFines = await _context.StudentFineCharges
                        .Where(sfc => sfc.StudentId == viewModel.StudentId && !sfc.IsPaid && sfc.IsActive)
                        .ToListAsync();

                    foreach (var fine in unpaidFines)
                    {
                        fine.IsPaid = true;
                        fine.PaidDate = DateTime.Now;
                        fine.BillingMasterId = billingMaster.Id;
                        fine.ModifiedBy = currentUser.FullName;
                        fine.ModifiedDate = DateTime.Now;
                    }

                    await _context.SaveChangesAsync();
                }

                // Handle salary deduction for employee parent students
                var categoryAssignment = await _context.StudentCategoryAssignments
                    .Include(sca => sca.StudentCategory)
                    .Where(sca => sca.StudentId == viewModel.StudentId && sca.IsActive)
                    .FirstOrDefaultAsync();

                if (categoryAssignment != null &&
                    categoryAssignment.StudentCategory.CategoryType == "EmployeeParent" &&
                    (categoryAssignment.PaymentMode == "CutFromSalary" || categoryAssignment.PaymentMode == "CustomRatio") &&
                    categoryAssignment.EmployeeId.HasValue &&
                    viewModel.SalaryDeductionAmount > 0)
                {
                    var salaryDeduction = new SalaryDeduction
                    {
                        StudentId = viewModel.StudentId,
                        EmployeeId = categoryAssignment.EmployeeId.Value,
                        BillingMasterId = billingMaster.Id,
                        AmountDeducted = viewModel.SalaryDeductionAmount,
                        ForMonth = viewModel.ForMonth,
                        ForYear = viewModel.ForYear,
                        DeductionDate = DateTime.Now,
                        CreatedBy = User.Identity.Name,
                        CampusId = (int)campusId
                    };
                    var SalaryDeductionbillingTransaction = new BillingTransaction
                    {
                        BillingMasterId = billingMaster.Id,
                        AmountPaid = viewModel.SalaryDeductionAmount,
                        CashPaid = 0,
                        OnlinePaid = 0,
                        OnlineAccount = null,
                        TransactionReference = "N/A",
                        PaymentDate = viewModel.PaymentDate,
                        ReceivedBy = "System-SalaryDeduction",
                        CampusId = (int)campusId,
                    };
                    _context.BillingTransactions.Add(SalaryDeductionbillingTransaction);
                    _context.SalaryDeductions.Add(salaryDeduction);
                    await _context.SaveChangesAsync();
                }

                // Open receipt in new tab and redirect current tab
                var receiptId = billingTransaction != null ? billingTransaction.Id : billingMaster.Id;
                var url = Url.Action("TransactionReceipt", "BillingReports", new { id = receiptId });
                var script = $"<script>window.open('{url}', '_blank'); window.location='{Url.Action("Index", "Billing")}';</script>";
                return Content(script, "text/html");

            }

            // Reload student name if validation fails
            viewModel.StudentName = student.StudentName;
            return View(viewModel);
        }
    }
}
