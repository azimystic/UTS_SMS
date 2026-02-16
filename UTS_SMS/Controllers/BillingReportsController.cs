using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using UTS_SMS.Services;
using UTS_SMS.ViewModels;

namespace UTS_SMS.Controllers
{
    public class BillingReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IExtraChargeService _extraChargeService;

        public BillingReportsController(ApplicationDbContext context, IUserService userService, IWebHostEnvironment env, UserManager<ApplicationUser> userManager, IExtraChargeService extraChargeService)
        {
            _context = context;
            _userService = userService;
            _env = env;
            _userManager = userManager;
            _extraChargeService = extraChargeService;
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
                .Select(c => new { id = c.Id, name = c.Name })
                .Distinct()
                .OrderBy(c => c.name)
                .ToListAsync();

            return Json(classes);
        }

        [HttpGet]
        public async Task<JsonResult> GetSectionsByClass(int? classId)
        {
            var sectionsQuery = _context.ClassSections.AsQueryable();

            if (classId.HasValue && classId.Value > 0)
            {
                sectionsQuery = sectionsQuery.Where(s => s.ClassId == classId.Value);
            }

            var sections = await sectionsQuery
                .Select(s => new { id = s.Id, name = s.Name })
                .Distinct()
                .OrderBy(s => s.name)
                .ToListAsync();

            return Json(sections);
        }
        // GET: BillingReports
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate,
            int? campusFilter, int? classFilter, int? sectionFilter, string statusFilter = "All", 
            string sortBy = "Name", string searchString = "")
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser.CampusId;

            // Set default dates if not provided
            if (!startDate.HasValue)
            {
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            
            if (!endDate.HasValue)
            {
                // Calculate end date based on start date
                endDate = new DateTime(startDate.Value.Year, startDate.Value.Month, 1)
                              .AddMonths(1)
                              .AddDays(-1);
            }

            // Set default campus filter to user's campus if not provided
            if (!campusFilter.HasValue && userCampusId.HasValue && userCampusId.Value > 0)
            {
                campusFilter = userCampusId.Value;
            }

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
               .OrderBy(c => c.Name)
               .ToListAsync();
            }

           

            // Section dropdown - filter by class if selected
            var sectionsQuery = _context.ClassSections.AsQueryable();
            if (classFilter.HasValue && classFilter.Value > 0)
            {
                sectionsQuery = sectionsQuery.Where(s => s.ClassId == classFilter.Value);

                ViewBag.Sections = await sectionsQuery
                    .OrderBy(s => s.Name)
                    .ToListAsync();
            }


            var query = _context.Students
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .Include(s => s.Campus)
                .Where(s => s.HasLeft == false && s.RegistrationDate <= endDate.Value)
                .AsQueryable();

            // Apply campus filter
            if (campusFilter.HasValue && campusFilter.Value > 0)
            {
                query = query.Where(s => s.CampusId == campusFilter.Value);
            }
            else if (userCampusId.HasValue && userCampusId.Value > 0)
            {
                // If user has a specific campus but no filter selected, show only that campus
                query = query.Where(s => s.CampusId == userCampusId.Value);
            }

            // Apply search filter (trim search string)
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(s => 
                    s.StudentName.Contains(searchString) ||
                    s.FatherCNIC.Contains(searchString) ||
                    s.StudentCNIC.Contains(searchString));
            }

            // Apply class filter
            if (classFilter.HasValue && classFilter.Value > 0)
            {
                query = query.Where(s => s.Class == classFilter.Value);
            }

            // Apply section filter
            if (sectionFilter.HasValue && sectionFilter.Value > 0)
            {
                query = query.Where(s => s.Section == sectionFilter.Value);
            }

            var students = await query.ToListAsync();
            var billingData = new List<BillingReportViewModel>();

            foreach (var student in students)
            {
                // Get billing records for the date range
                var billingRecords = await _context.BillingMaster
                    .Include(b => b.Transactions)
                    .Where(b => b.StudentId == student.Id &&
                               ((b.ForYear > startDate.Value.Year) ||
                                (b.ForYear == startDate.Value.Year && b.ForMonth >= startDate.Value.Month)) &&
                               ((b.ForYear < endDate.Value.Year) ||
                                (b.ForYear == endDate.Value.Year && b.ForMonth <= endDate.Value.Month)))
                    .ToListAsync();

                decimal totalPayable, totalPaid, balance;
                string status;

                if (billingRecords.Any())
                {
                    // ✅ Normal calculation
                    totalPayable = billingRecords.Sum(b => b.TuitionFee + b.AdmissionFee + b.Fine + b.PreviousDues + b.MiscallaneousCharges);
                    totalPaid = billingRecords.SelectMany(b => b.Transactions).Sum(t => t.AmountPaid);

                    var lastBilling = billingRecords
                        .OrderByDescending(b => b.ForYear)
                        .ThenByDescending(b => b.ForMonth)
                        .FirstOrDefault();

                    balance = totalPayable - totalPaid;

                    if (totalPayable == 0 && totalPaid == 0)
                        status = "NotProcessed";
                    else if (balance == 0)
                        status = "Paid";
                    else if (balance < 0)
                        status = "Advance";
                    else
                        status = "NotPaid"; // Maps both "Partial" and "Unpaid" to NotPaid
                }
                else
                {
                    // ✅ No billing record → calculate fees manually like in Create()
                    var classFee = await _context.ClassFees
                        .FirstOrDefaultAsync(cf => cf.ClassId == student.ClassObj.Id);

                    if (classFee == null) continue;

                    var tuitionFee = classFee.TuitionFee * (1 - ((student.TuitionFeeDiscountPercent ?? 0) / 100m));
                    
                    // Calculate extra charges
                    var extraCharges = await _extraChargeService.CalculateExtraCharges(student.ClassObj.Id, student.Id, student.CampusId);
                    
                    // Get unpaid fines
                    var unpaidFines = await _context.StudentFineCharges
                        .Where(sfc => sfc.StudentId == student.Id && !sfc.IsPaid && sfc.IsActive)
                        .SumAsync(sfc => (decimal?)sfc.Amount) ?? 0;
                    
                    var misc = extraCharges + unpaidFines;

                    var admissionFee = student.AdmissionFeePaid ? 0 :
                Math.Max(0, classFee.AdmissionFee - (classFee.AdmissionFee * ((student.AdmissionFeeDiscountAmount ?? 0) / 100m)));

                    // Previous dues from last billing record
                    var lastRecord = await _context.BillingMaster
                        .Where(b => b.StudentId == student.Id)
                        .OrderByDescending(b => b.ForYear)
                        .ThenByDescending(b => b.ForMonth)
                        .FirstOrDefaultAsync();

                    var previousDues = lastRecord?.Dues ?? 0;

                    totalPayable = tuitionFee + admissionFee + previousDues + misc;
                    totalPaid = 0;
                    balance = totalPayable;

                    status = "NotPaid";
                }

                // Apply status filter
                if (statusFilter != "All" && status != statusFilter)
                    continue;

                billingData.Add(new BillingReportViewModel
                {
                    Student = student,
                    TotalPayable = totalPayable,
                    TotalPaid = totalPaid,
                    Balance = balance,
                    Status = status,
                    BillingRecords = billingRecords
                });
            }

            // Apply sorting
            billingData = sortBy switch
            {
                "Class" => billingData.OrderBy(b => b.Student.ClassObj?.Name).ThenBy(b => b.Student.SectionObj?.Name).ToList(),
                "Section" => billingData.OrderBy(b => b.Student.SectionObj?.Name).ThenBy(b => b.Student.StudentName).ToList(),
                "Status" => billingData.OrderBy(b => b.Status).ThenBy(b => b.Student.StudentName).ToList(),
                "Balance" => billingData.OrderByDescending(b => b.Balance).ToList(),
                _ => billingData.OrderBy(b => b.Student.StudentName).ToList()
            };

            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.CampusFilter = campusFilter;
            ViewBag.ClassFilter = classFilter;
            ViewBag.SectionFilter = sectionFilter;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.SortBy = sortBy;
            ViewBag.SearchString = searchString;

            return View(billingData);
        }


        // GET: BillingReports/Details/5
        public async Task<IActionResult> Details(int id, DateTime? startDate, DateTime? endDate)
        {
            if (id == 0)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser?.StudentId == null)
                {
                    return RedirectToAction("Logout", "Account");
                }
                else
                {
                    id = (int)currentUser?.StudentId;
                }
            }
            var student = await _context.Students
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .Include(s => s.Campus)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return NotFound();

            if (!startDate.HasValue) startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            if (!endDate.HasValue) endDate = startDate.Value.AddMonths(1).AddDays(-1);

            var billingRecords = await _context.BillingMaster
                .Include(b => b.Transactions)
                    .ThenInclude(t => t.Account)
                .Include(b => b.Campus)
                .Where(b => b.StudentId == student.Id)
                .OrderByDescending(b => b.ForYear).ThenByDescending(b => b.ForMonth)
                .ToListAsync();

            // ✅ Get extra charges for each billing record
            var billingExtraCharges = new Dictionary<int, List<dynamic>>();
            foreach (var billing in billingRecords)
            {
                var extraChargePayments = await _context.ClassFeeExtraChargePaymentHistories
                    .Where(ph => ph.BillingMasterId == billing.Id)
                    .Include(ph => ph.ClassFeeExtraCharge)
                    .Select(ph => new
                    {
                        ChargeName = ph.ClassFeeExtraCharge.ChargeName,
                        Amount = ph.AmountPaid,
                        Category = ph.ClassFeeExtraCharge.Category,
                        Type = "ExtraCharge"
                    })
                    .ToListAsync();

                // Add student fines/charges that were paid in this billing
                var fineCharges = await _context.StudentFineCharges
                    .Where(sfc => sfc.BillingMasterId == billing.Id && sfc.IsPaid)
                    .Select(sfc => new
                    {
                        ChargeName = sfc.ChargeName,
                        Amount = sfc.Amount,
                        Category = "Fine/Charge",
                        Type = "Fine"
                    })
                    .ToListAsync();

                billingExtraCharges[billing.Id] = extraChargePayments.Cast<dynamic>().Concat(fineCharges.Cast<dynamic>()).ToList();
            }

            ViewBag.BillingExtraCharges = billingExtraCharges;

            var viewModel = new BillingDetailReportViewModel
            {
                Student = student,
                BillingRecords = billingRecords,
                StartDate = startDate.Value,
                EndDate = endDate.Value
            };

            return View(viewModel);
        }

        public async Task<IActionResult> TransactionReceipt(int id)
        {
            var transaction = await _context.BillingTransactions
                .Include(t => t.BillingMaster)
                    .ThenInclude(b => b.Student)
                        .ThenInclude(s => s.ClassObj)
                .Include(t => t.BillingMaster)
                    .ThenInclude(b => b.Student)
                        .ThenInclude(s => s.SectionObj)
                .Include(t => t.Campus)
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
                return NotFound();

            // 🟢 Calculate Already Paid (all transactions for same BillingMaster before this one)
            var alreadyPaid = await _context.BillingTransactions
                .Where(t => t.BillingMasterId == transaction.BillingMasterId && t.Id < transaction.Id)
                .SumAsync(t => (decimal?)t.AmountPaid) ?? 0;

            // Get extra charges for this billing
            var extraChargePayments = await _context.ClassFeeExtraChargePaymentHistories
                .Where(ph => ph.BillingMasterId == transaction.BillingMasterId)
                .Include(ph => ph.ClassFeeExtraCharge)
                .ToListAsync();

            var extraChargeItems = extraChargePayments.Select(ph => new
            {
                ChargeName = ph.ClassFeeExtraCharge.ChargeName,
                Amount = ph.AmountPaid,
                Category = ph.ClassFeeExtraCharge.Category,
                Type = "ExtraCharge"
            }).ToList();

            // Add student fines/charges
            var fineCharges = await _context.StudentFineCharges
                .Where(sfc => sfc.BillingMasterId == transaction.BillingMasterId && sfc.IsPaid)
                .Select(sfc => new
                {
                    ChargeName = sfc.ChargeName,
                    Amount = sfc.Amount,
                    Category = "Fine/Charge",
                    Type = "Fine"
                })
                .ToListAsync();

            var allCharges = extraChargeItems.Concat(fineCharges).ToList();
            var extraChargesTotal = allCharges.Sum(i => i.Amount);

            ViewBag.AlreadyPaid = alreadyPaid;
            ViewBag.PrintTime = DateTime.Now;
            ViewBag.ExtraChargesTotal = extraChargesTotal;
            ViewBag.ExtraChargeItems = allCharges;

            return View(transaction);
        }


        // GET: BillingReports/PrintReport
        // GET: BillingReports/PrintReport
        public async Task<IActionResult> PrintReport(DateTime? startDate, DateTime? endDate,
            int? classFilter, int? sectionFilter, string statusFilter = "All", string sortBy = "Name")
        {
            // Default date range
            if (!startDate.HasValue) startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            if (!endDate.HasValue) endDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)
                                                .AddMonths(1).AddDays(-1);

            var query = _context.Students
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .Include(s => s.Campus)
                .Where(s => s.HasLeft == false && s.RegistrationDate <= endDate.Value)
                .AsQueryable();

            // Filters
            if (classFilter.HasValue && classFilter.Value > 0)
                query = query.Where(s => s.Class == classFilter.Value);

            if (sectionFilter.HasValue && sectionFilter.Value > 0)
                query = query.Where(s => s.Section == sectionFilter.Value);

            var students = await query.ToListAsync();
            var billingData = new List<BillingReportViewModel>();

            foreach (var student in students)
            {
                var billingRecords = await _context.BillingMaster
                    .Include(b => b.Transactions)
                    .Where(b => b.StudentId == student.Id &&
                               ((b.ForYear > startDate.Value.Year) ||
                                (b.ForYear == startDate.Value.Year && b.ForMonth >= startDate.Value.Month)) &&
                               ((b.ForYear < endDate.Value.Year) ||
                                (b.ForYear == endDate.Value.Year && b.ForMonth <= endDate.Value.Month)))
                    .ToListAsync();

                decimal totalPayable, totalPaid, balance;
                string status;

                if (billingRecords.Any())
                {
                    // ✅ Normal calculation if billing exists
                    totalPayable = billingRecords.Sum(b => b.TuitionFee + b.AdmissionFee + b.Fine + b.PreviousDues + b.MiscallaneousCharges);
                    totalPaid = billingRecords.SelectMany(b => b.Transactions).Sum(t => t.AmountPaid);

                    var lastBilling = billingRecords
                        .OrderByDescending(b => b.ForYear)
                        .ThenByDescending(b => b.ForMonth)
                        .FirstOrDefault();

                    balance = lastBilling?.Dues ?? 0;

                    if (totalPayable == 0 && totalPaid == 0)
                        status = "NotProcessed";
                    else if (balance == 0)
                        status = "Paid";
                    else if (balance < 0)
                        status = "Advance";
                    else
                        status = "NotPaid"; // Maps both "Partial" and "Unpaid" to NotPaid
                }
                else
                {
                    // ✅ No billing record → calculate fees manually like in Create()
                    var classFee = await _context.ClassFees
                        .FirstOrDefaultAsync(cf => cf.ClassId == student.ClassObj.Id);

                    if (classFee == null) continue;

                    var tuitionFee = classFee.TuitionFee * (1 - ((student.TuitionFeeDiscountPercent ?? 0) / 100m));
                    
                    // Calculate extra charges
                    var extraCharges = await _extraChargeService.CalculateExtraCharges(student.ClassObj.Id, student.Id, student.CampusId);
                    
                    // Get unpaid fines
                    var unpaidFines = await _context.StudentFineCharges
                        .Where(sfc => sfc.StudentId == student.Id && !sfc.IsPaid && sfc.IsActive)
                        .SumAsync(sfc => (decimal?)sfc.Amount) ?? 0;
                    
                    var misc = extraCharges + unpaidFines;

                    // Check if AdmissionNextMonth is enabled - if so, skip admission fee in print reports
                    var admissionFee = (student.AdmissionFeePaid) ? 0 :
      Math.Max(0, classFee.AdmissionFee * (1 - ((student.AdmissionFeeDiscountAmount ?? 0) / 100m)));

                    // get previous dues
                    var lastRecord = await _context.BillingMaster
                        .Where(b => b.StudentId == student.Id)
                        .OrderByDescending(b => b.ForYear)
                        .ThenByDescending(b => b.ForMonth)
                        .FirstOrDefaultAsync();

                    var previousDues = lastRecord?.Dues ?? 0;

                    totalPayable = tuitionFee + admissionFee + previousDues + misc;
                    totalPaid = 0;
                    balance = totalPayable;

                    status = "NotPaid";
                }

                if (statusFilter != "All" && status != statusFilter)
                    continue;

                billingData.Add(new BillingReportViewModel
                {
                    Student = student,
                    TotalPayable = totalPayable,
                    TotalPaid = totalPaid,
                    Balance = balance,
                    Status = status,
                    BillingRecords = billingRecords
                });
            }


            // Sorting
            billingData = sortBy switch
            {
                "Class" => billingData.OrderBy(b => b.Student.ClassObj?.Name)
                                      .ThenBy(b => b.Student.SectionObj?.Name).ToList(),
                "Section" => billingData.OrderBy(b => b.Student.SectionObj?.Name)
                                        .ThenBy(b => b.Student.StudentName).ToList(),
                "Status" => billingData.OrderBy(b => b.Status)
                                       .ThenBy(b => b.Student.StudentName).ToList(),
                "Balance" => billingData.OrderByDescending(b => b.Balance).ToList(),
                _ => billingData.OrderBy(b => b.Student.StudentName).ToList()
            };

            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.Campus = await _context.Campuses.FirstOrDefaultAsync();
            ViewBag.StatusFilter = statusFilter;
            ViewBag.SortBy = sortBy;

            return View(billingData);
        }
        // GET: BillingReports/Transactions
        public async Task<IActionResult> Transactions(DateTime? date)
        {
            var selectedDate = date ?? DateTime.Today;
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser.CampusId;
            var transactions = await _context.BillingTransactions
                .Include(t => t.BillingMaster)
                    .ThenInclude(b => b.Student)
                        .ThenInclude(s => s.ClassObj)
                .Include(t => t.BillingMaster)
                    .ThenInclude(b => b.Student)
                        .ThenInclude(s => s.SectionObj)
                .Include(t => t.Campus)
                .Where(t => t.PaymentDate.Date == selectedDate.Date && 
                           !t.ReceivedBy.StartsWith("System-SalaryDeduction"))
                .OrderBy(t => t.BillingMaster.Student.ClassObj.Name)
                .ThenBy(t => t.BillingMaster.Student.StudentName)
                .ThenBy(t => t.PaymentDate)
                .ToListAsync();
            if (userCampusId.HasValue && userCampusId.Value > 0)
            {
                transactions = transactions.Where(c => c.CampusId == userCampusId.Value).ToList();
            }
            var gradeGroups = transactions
                .GroupBy(t => t.BillingMaster.Student.ClassObj)
                .OrderBy(g => g.Key?.Name)
                .Select(g => new GradeTransactionGroup
                {
                    ClassObj = g.Key,
                    StudentGroups = g
                        .GroupBy(t => t.BillingMaster.Student)
                        .OrderBy(sg => sg.Key?.StudentName)
                        .Select(sg => new StudentTransactionGroup
                        {
                            Student = sg.Key,
                            Transactions = sg.OrderBy(t => t.PaymentDate).ToList()
                        })
                        .ToList()
                })
                .ToList();

            var viewModel = new DailyTransactionsViewModel
            {
                SelectedDate = selectedDate,
                Campus = await _context.Campuses.FirstOrDefaultAsync(),
                GradeGroups = gradeGroups
            };

            return View(viewModel);
        }

        // GET: BillingReports/PrintTransactions
        public async Task<IActionResult> PrintTransactions(DateTime? date)
        {
            var selectedDate = date ?? DateTime.Today;
            var currentUser = await _userManager.GetUserAsync(User);
            var userCampusId = currentUser.CampusId;
            
            var transactions = await _context.BillingTransactions
                .Include(t => t.BillingMaster)
                    .ThenInclude(b => b.Student)
                        .ThenInclude(s => s.ClassObj)
                .Include(t => t.BillingMaster)
                    .ThenInclude(b => b.Student)
                        .ThenInclude(s => s.SectionObj)
                .Include(t => t.Campus)
                .Where(t => t.PaymentDate.Date == selectedDate.Date && 
                           !t.ReceivedBy.StartsWith("System-SalaryDeduction"))
                .OrderBy(t => t.BillingMaster.Student.ClassObj.Name)
                .ThenBy(t => t.BillingMaster.Student.StudentName)
                .ThenBy(t => t.PaymentDate)
                .ToListAsync();
                
            if (userCampusId.HasValue && userCampusId.Value > 0)
            {
                transactions = transactions.Where(c => c.CampusId == userCampusId.Value).ToList();
            }

            // ✅ Load extra charges and fines breakdown for each transaction
            var transactionIds = transactions.Select(t => t.Id).ToList();
            var billingIds = transactions.Select(t => t.BillingMasterId).Distinct().ToList();
            
            var transactionBreakdowns = new Dictionary<int, List<dynamic>>();
            
            foreach (var transactionId in transactionIds)
            {
                var transaction = transactions.First(t => t.Id == transactionId);
                var billingId = transaction.BillingMasterId;
                
                // Get extra charges
                var extraCharges = await _context.ClassFeeExtraChargePaymentHistories
                    .Where(ph => ph.BillingMasterId == billingId)
                    .Include(ph => ph.ClassFeeExtraCharge)
                    .Select(ph => new
                    {
                        ChargeName = ph.ClassFeeExtraCharge.ChargeName,
                        Amount = ph.AmountPaid,
                        Category = ph.ClassFeeExtraCharge.Category,
                        Type = "ExtraCharge"
                    })
                    .ToListAsync();
                
                // Get fines
                var fines = await _context.StudentFineCharges
                    .Where(sfc => sfc.BillingMasterId == billingId && sfc.IsPaid)
                    .Select(sfc => new
                    {
                        ChargeName = sfc.ChargeName,
                        Amount = sfc.Amount,
                        Category = "Fine/Charge",
                        Type = "Fine"
                    })
                    .ToListAsync();
                
                transactionBreakdowns[transactionId] = extraCharges.Cast<dynamic>()
                    .Concat(fines.Cast<dynamic>())
                    .ToList();
            }
            
            ViewBag.TransactionBreakdowns = transactionBreakdowns;
            
            var gradeGroups = transactions
                .GroupBy(t => t.BillingMaster.Student.ClassObj)
                .OrderBy(g => g.Key?.Name)
                .Select(g => new GradeTransactionGroup
                {
                    ClassObj = g.Key,
                    StudentGroups = g
                        .GroupBy(t => t.BillingMaster.Student)
                        .OrderBy(sg => sg.Key?.StudentName)
                        .Select(sg => new StudentTransactionGroup
                        {
                            Student = sg.Key,
                            Transactions = sg.OrderBy(t => t.PaymentDate).ToList()
                        })
                        .ToList()
                })
                .ToList();

            var viewModel = new DailyTransactionsViewModel
            {
                SelectedDate = selectedDate,
                Campus = await _context.Campuses.FirstOrDefaultAsync(),
                GradeGroups = gradeGroups
            };

            return View(viewModel);
        }

        // GET: BillingReports/SearchInvoice
        public IActionResult SearchInvoice()
        {
            return View();
        }

        // POST: BillingReports/SearchInvoice
        [HttpPost]
        public async Task<IActionResult> SearchInvoice(string invoiceType, int invoiceNumber)
        {
            if (string.IsNullOrEmpty(invoiceType) || invoiceNumber <= 0)
            {
                return Json(new { success = false, message = "Please select invoice type and enter a valid invoice number." });
            }

            if (invoiceType == "FeeInvoice")
            {
                // Check if billing transaction exists
                var transaction = await _context.BillingTransactions
                    .FirstOrDefaultAsync(t => t.Id == invoiceNumber);

                if (transaction == null)
                {
                    return Json(new { success = false, message = $"Fee Invoice #{invoiceNumber} does not exist." });
                }

                // Return URL to open receipt in new tab
                var url = Url.Action("TransactionReceipt", "BillingReports", new { id = invoiceNumber });
                return Json(new { success = true, url = url });
            }
            else if (invoiceType == "PayrollInvoice")
            {
                // Check if payroll transaction exists
                var payrollTransaction = await _context.PayrollTransactions
                    .FirstOrDefaultAsync(pt => pt.PayrollMasterId == invoiceNumber);

                if (payrollTransaction == null)
                {
                    return Json(new { success = false, message = $"Payroll Invoice #{invoiceNumber} does not exist." });
                }

                // Return URL to open receipt in new tab
                var url = Url.Action("TransactionReceipt", "PayrollReports", new { id = invoiceNumber });
                return Json(new { success = true, url = url });
            }

            return Json(new { success = false, message = "Invalid invoice type selected." });
        }
    }
    }