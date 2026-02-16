using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using SMS.Services;

namespace SMS.Controllers
{   // Student Dashboard Controller
    [Authorize(Roles = "Admin,Student")]
    public class StudentDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IExtraChargeService _extraChargeService;

        public StudentDashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IExtraChargeService extraChargeService)
        {
            _context = context;
            _userManager = userManager;
            _extraChargeService = extraChargeService;
        }

        public async Task<IActionResult> Dashboard(int? studentId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            int student_Id = 0;
            if (currentUser != null)
            {
                if(currentUser.StudentId == null)
                {
                    student_Id = (int)studentId;
                }
                else {
                    student_Id = (int)currentUser.StudentId;
                }
            }
            else
            {
                student_Id = (int)studentId;
            }
                var student = await _context.Students
                    .Include(s => s.ClassObj)
                    .Include(s => s.SectionObj)
                    .Include(s => s.SubjectsGrouping)
                    .Include(s => s.Campus)
                    .Include(s => s.StudentCategory)
                    .FirstOrDefaultAsync(s => s.Id == student_Id);

            if (student == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var model = new StudentDashboardViewModel
            {
                Student = student,
                StudentName = student.StudentName,
                StudentId = student.Id,
                ClassName = student.ClassObj?.Name ?? "N/A",
                SectionName = student.SectionObj?.Name ?? "N/A",
                ProfilePicture = student.ProfilePicture,
                
                // Attendance data
                RecentAttendance = await _context.Attendance
                    .Where(a => a.StudentId == student.Id)
                    .OrderByDescending(a => a.Date)
                    .Take(10)
                    .ToListAsync(),
                AttendancePercentage = await GetAttendancePercentage(student.Id),
                AttendanceTrends = await GetAttendanceTrends(student.Id),
                
                // Fee/Billing data
                RecentBilling = await _context.BillingTransactions
                    .Where(t => _context.BillingMaster
                        .Any(m => m.Id == t.BillingMasterId && m.StudentId == student.Id))
                    .OrderByDescending(t => t.PaymentDate)
                    .Take(5)
                    .ToListAsync(),
                FeeDetails = await GetFeeDetails(student.Id),
                
                // Assigned duties
                AssignedDuties = await GetAssignedDuties(student.Id),
                
                // To-do list
                ToDoList = await GetToDoList(student.Id),
                
                // Homework/Diary
                TodayHomework = await GetTodayHomework(student.Id),
                
                // Timetable - Today and Tomorrow
                TodayTimetable = await GetTimetableForDate(student.Class, student.Section, DateTime.Today),
                TomorrowTimetable = await GetTimetableForDate(student.Class, student.Section, DateTime.Today.AddDays(1)),
                
                // Calendar events
                CalendarEvents = await GetCalendarEvents(student.CampusId),
                
                // Complaints
                Complaints = await GetStudentComplaintsPrivate(student.Id),
                
                // Exam analysis
                ExamAnalysis = await GetExamAnalysis(student.Id),
                
                // Student Awards/Positions
                StudentAwards = await GetStudentAwards(student.Id),
                
                // Teacher of the Month (previous month's winner)
                TeacherOfMonth = await GetPreviousMonthTopTeacher(student.CampusId)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddTodo([FromBody] TodoRequest request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var todo = new ToDo
                {
                    Title = request.Title,
                    Description = request.Description,
                    DueDate = request.DueDate,
                    Priority = request.Priority,
                    UserId = currentUser.Id,
                    CampusId = currentUser.CampusId,
                    CreatedBy = currentUser.UserName,
                    IsActive = true,
                    IsCompleted = false
                };

                _context.ToDos.Add(todo);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "To-do item added successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to add to-do item: " + ex.Message });
            }
        }

        public class TodoRequest
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public DateTime DueDate { get; set; }
            public string Priority { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> GetCalendarEventsByMonth(int year, int month)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var events = await _context.CalendarEvents
                    .Where(e => e.CampusId == currentUser.CampusId 
                        && e.StartDate >= startDate 
                        && e.StartDate <= endDate)
                    .OrderBy(e => e.StartDate)
                    .Select(e => new
                    {
                        id = e.Id,
                        title = e.EventName,
                        description = e.Description,
                        startDate = e.StartDate.ToString("yyyy-MM-dd"),
                        endDate = e.EndDate,
                        isHoliday = e.IsHoliday
                    })
                    .ToListAsync();

                return Json(new { success = true, events = events });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to fetch events: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentDuties(int studentId)
        {
            try
            {
                var duties = await _context.AssignedDuties
                    .Include(d => d.AssignedStudents)
                    .Where(d => d.AssignedStudents.Any(ads => ads.StudentId == studentId && ads.IsActive))
                    .Where(d => d.IsActive && d.Status != "Completed" && d.Status != "Cancelled")
                    .OrderByDescending(d => d.Priority == "High" ? 3 : d.Priority == "Medium" ? 2 : 1)
                    .ThenBy(d => d.StartDate)
                    .Take(5)
                    .Select(d => new
                    {
                        d.Id,
                        d.DutyTitle,
                        d.Description,
                        d.Priority,
                        d.StartDate,
                        d.Status
                    })
                    .ToListAsync();

                return Json(new { success = true, duties });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to fetch duties: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentComplaints(int studentId)
        {
            try
            {
                var complaints = await _context.StudentComplaints
                    .Where(c => c.StudentId == studentId 
                        && c.IsActive 
                        && c.Status == "Open"
                        && (c.ReporterType == "Admin" || c.ReporterType == "Teacher"))
                    .OrderByDescending(c => c.ComplaintDate)
                    .Take(5)
                    .Select(c => new
                    {
                        c.Id,
                        c.ComplaintTitle,
                        c.ComplaintDescription,
                        c.ComplaintType,
                        c.ComplaintDate,
                        c.ReporterType
                    })
                    .ToListAsync();

                return Json(new { success = true, complaints });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to fetch complaints: " + ex.Message });
            }
        }


        private async Task<double> GetAttendancePercentage(int studentId)
        {
            var totalDays = await _context.Attendance.CountAsync(a => a.StudentId == studentId);
            if (totalDays == 0) return 0;

            var presentDays = await _context.Attendance.CountAsync(a => a.StudentId == studentId && a.Status == "P");
            return Math.Round((double)presentDays / totalDays * 100, 2);
        }
        
        private async Task<AttendanceTrendData> GetAttendanceTrends(int studentId)
        {
            var now = DateTime.Now;
            var thisMonth = await GetMonthlyAttendance(studentId, now.Year, now.Month);
            var lastMonth = await GetMonthlyAttendance(studentId, now.AddMonths(-1).Year, now.AddMonths(-1).Month);
            var lastYear = await GetYearlyAttendance(studentId, now.Year - 1);
            
            return new AttendanceTrendData
            {
                ThisMonth = thisMonth,
                LastMonth = lastMonth,
                LastYear = lastYear
            };
        }
        
        private async Task<AttendanceData> GetMonthlyAttendance(int studentId, int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            
            var records = await _context.Attendance
                .Where(a => a.StudentId == studentId && a.Date >= startDate && a.Date <= endDate)
                .ToListAsync();
            
            var total = records.Count;
            var present = records.Count(a => a.Status == "P");
            
            return new AttendanceData
            {
                Total = total,
                Present = present,
                Absent = total - present,
                Percentage = total > 0 ? Math.Round((double)present / total * 100, 2) : 0
            };
        }
        
        private async Task<AttendanceData> GetYearlyAttendance(int studentId, int year)
        {
            var startDate = new DateTime(year, 1, 1);
            var endDate = new DateTime(year, 12, 31);
            
            var records = await _context.Attendance
                .Where(a => a.StudentId == studentId && a.Date >= startDate && a.Date <= endDate)
                .ToListAsync();
            
            var total = records.Count;
            var present = records.Count(a => a.Status == "P");
            
            return new AttendanceData
            {
                Total = total,
                Present = present,
                Absent = total - present,
                Percentage = total > 0 ? Math.Round((double)present / total * 100, 2) : 0
            };
        }
        
        private async Task<FeeDetailsData> GetFeeDetails(int studentId)
        {
            var student = await _context.Students
                .Include(s => s.ClassObj)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null) return new FeeDetailsData();

            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            // Get current month billing
            var billingMaster = await _context.BillingMaster
                .Include(b => b.Transactions)
                .FirstOrDefaultAsync(b => b.StudentId == studentId 
                    && b.ForMonth == currentMonth 
                    && b.ForYear == currentYear);

            var feeCalculation = new FeeCalculationData();

            if (billingMaster != null)
            {
                var fineCharges = await _context.StudentFineCharges
                        .Where(sfc => sfc.StudentId == studentId && !sfc.IsPaid && sfc.IsActive)
                        .SumAsync(sfc => (decimal?)sfc.Amount) ?? 0;
                // Billing exists - get data from billing master
                feeCalculation.TotalPayable = billingMaster.TuitionFee + 
                                              billingMaster.AdmissionFee + 
                                              billingMaster.MiscallaneousCharges + 
                                              billingMaster.Fine + 
                                              billingMaster.PreviousDues +
                                              fineCharges;
                feeCalculation.TotalPaid = billingMaster.Transactions?.Sum(t => t.AmountPaid) ?? 0;
                feeCalculation.Transactions = billingMaster.Transactions?.ToList() ?? new List<BillingTransaction>();
            }
            else
            {
                // No billing - calculate from ClassFee and student discounts
                var classFee = await _context.ClassFees
                    .FirstOrDefaultAsync(cf => cf.ClassId == student.ClassObj.Id);

                if (classFee != null)
                {
                    // Calculate tuition with discount
                    var tuitionFee = classFee.TuitionFee * (1 - ((student.TuitionFeeDiscountPercent ?? 0) / 100m));
                    
                    // Calculate admission fee if not paid
                    var admissionFee = student.AdmissionFeePaid ? 0 :
                        Math.Max(0, classFee.AdmissionFee - (classFee.AdmissionFee * ((student.AdmissionFeeDiscountAmount ?? 0) / 100m)));

                    // Get unpaid fines
                    var fineCharges = await _context.StudentFineCharges
                        .Where(sfc => sfc.StudentId == studentId && !sfc.IsPaid && sfc.IsActive)
                        .SumAsync(sfc => (decimal?)sfc.Amount) ?? 0;
                    var extraChargesAmount = await _extraChargeService.CalculateExtraCharges(student.Class, student.Id, student.CampusId);
                    // --- Step 2: Fetch and Calculate Dues Logic (Improved) ---
                    // Get the last billing record before the current month
                    var lastRecord = await _context.BillingMaster
                        .Include(b => b.Transactions)
                        .Where(b => b.StudentId == student.Id &&
                                   (b.ForYear < currentYear || (b.ForYear == currentYear && b.ForMonth < currentMonth)))
                        .OrderByDescending(b => b.ForYear)
                        .ThenByDescending(b => b.ForMonth)
                        .FirstOrDefaultAsync();
                    decimal calculatedPreviousDues = 0;
                    if (lastRecord != null)
                    {
                        // Calculate Previous Dues = TotalPayable from last bill - Sum of all transactions for that bill
                        var lastBillTotalPayable = lastRecord.TuitionFee + lastRecord.AdmissionFee +
                                                   lastRecord.MiscallaneousCharges + lastRecord.PreviousDues + lastRecord.Fine;
                        var lastBillTotalPaid = lastRecord.Transactions?.Sum(t => t.AmountPaid) ?? 0;
                        calculatedPreviousDues = lastBillTotalPayable - lastBillTotalPaid;

                        var lastRecordDate = new DateTime(lastRecord.ForYear, lastRecord.ForMonth, 1);
                        var selectedDate = new DateTime(currentYear, currentMonth, 1);

                        var monthsGap = ((selectedDate.Year - lastRecordDate.Year) * 12)
                                        + selectedDate.Month - lastRecordDate.Month - 1;

                        var lastMonthName = System.Globalization.DateTimeFormatInfo.CurrentInfo.GetMonthName(lastRecord.ForMonth);

                        if (monthsGap > 0)
                        {
                            // Add missing months' fees (tuition + extra charges for each missing month)
                            var missingMonthsAmount = monthsGap * (tuitionFee + extraChargesAmount);
                            calculatedPreviousDues += missingMonthsAmount;

                         }
                        
                    }
                    feeCalculation.TotalPayable = tuitionFee + admissionFee + fineCharges + extraChargesAmount + calculatedPreviousDues;
                }
                
                feeCalculation.TotalPaid = 0;
                feeCalculation.Transactions = new List<BillingTransaction>();
            }

            feeCalculation.RemainingDues = feeCalculation.TotalPayable - feeCalculation.TotalPaid;
            feeCalculation.HasBillingRecord = billingMaster != null;
            feeCalculation.BillingMasterId = billingMaster?.Id;

            return new FeeDetailsData
            {
                ThisMonthFee = billingMaster,
                FeeCalculation = feeCalculation
            };
        }
        
        private async Task<List<AssignedDuty>> GetAssignedDuties(int studentId)
        {
            var duties = await _context.AssignedDuties
                .Include(d => d.Employee)
                .Include(d => d.AssignedStudents)
                    .ThenInclude(ads => ads.Student)
                .Where(d => d.AssignedStudents.Any(ads => ads.StudentId == studentId && ads.IsActive))
                .Where(d => d.IsActive && d.Status != "Completed" && d.Status != "Cancelled")
                .OrderByDescending(d => d.Priority == "High" ? 3 : d.Priority == "Medium" ? 2 : 1)
                .ThenBy(d => d.StartDate)
                .ToListAsync();
            
            return duties;
        }
        
        private async Task<List<ToDoItemData>> GetToDoList(int studentId)
        {
            // Get the user associated with this student
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.StudentId == studentId);
            if (user == null) return new List<ToDoItemData>();
            
            var todos = await _context.ToDos
                .Where(t => t.UserId == user.Id && t.IsActive)
                .OrderBy(t => t.IsCompleted)
                .ThenBy(t => t.DueDate)
                .Take(10)
                .Select(t => new ToDoItemData
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description ?? "",
                    IsCompleted = t.IsCompleted,
                    DueDate = t.DueDate,
                    Priority = t.Priority
                })
                .ToListAsync();
            
            return todos;
        }
        
        private async Task<List<HomeworkData>> GetTodayHomework(int studentId)
        {
            var student = await _context.Students
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .Include(s => s.SubjectsGrouping)
                    .ThenInclude(sg => sg.SubjectsGroupingDetails)
                        .ThenInclude(sig => sig.Subject)
                .FirstOrDefaultAsync(s => s.Id == studentId);
            
            if (student == null) return new List<HomeworkData>();
            
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var diaries = await _context.Diaries
                .Include(d => d.TeacherAssignment)
                    .ThenInclude(ta => ta.Subject)
                .Include(d => d.TeacherAssignment)
                    .ThenInclude(ta => ta.Teacher)
                .Include(d => d.DiaryImages)
                .Where(d => d.Date >= today && d.Date < tomorrow
                    && d.TeacherAssignment.ClassId == student.Class 
                    && d.TeacherAssignment.SectionId == student.Section)
                .ToListAsync();
            
            var homeworkList = diaries.Select(d => new HomeworkData
            {
                Id = d.Id,
                SubjectName = d.TeacherAssignment.Subject.Name,
                TeacherName = d.TeacherAssignment.Teacher.FullName,
                LessonSummary = d.LessonSummary,
                Homework = d.HomeworkGiven,
                Notes = d.Notes,
                Date = d.Date,
                Images = d.DiaryImages.Select(di => di.ImagePath).ToList()
            }).ToList();
            
            return homeworkList;
        }
        
        private async Task<List<CalendarEventData>> GetCalendarEvents(int campusId)
        {
            var events = await _context.CalendarEvents
                .Where(e => e.CampusId == campusId && e.StartDate >= DateTime.Today)
                .OrderBy(e => e.StartDate)
                .Take(10)
                .Select(e => new CalendarEventData
                {
                    Id = e.Id,
                    Title = e.EventName,
                    Description = e.Description,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    IsHoliday = e.IsHoliday
                })
                .ToListAsync();
            
            return events;
        }
        
        private async Task<List<ComplaintData>> GetStudentComplaintsPrivate(int studentId)
        {
            var complaints = await _context.StudentComplaints
                .Where(c => c.StudentId == studentId)
                .OrderByDescending(c => c.ComplaintDate)
                .Take(10)
                .Select(c => new ComplaintData
                {
                    Id = c.Id,
                    Title = c.ComplaintTitle,
                    Description = c.ComplaintDescription,
                    Status = c.Status,
                    ComplaintDate = c.ComplaintDate,
                    ResolvedDate = c.ResolvedDate
                })
                .ToListAsync();
            
            return complaints;
        }
        
        private async Task<ExamAnalysisData> GetExamAnalysis(int studentId)
        {
            var student = await _context.Students
                .Include(s => s.SubjectsGrouping)
                    .ThenInclude(sg => sg.SubjectsGroupingDetails)
                .FirstOrDefaultAsync(s => s.Id == studentId);
            
            if (student == null) return new ExamAnalysisData();
            
            // Get student's subject IDs from grouping
            var studentSubjectIds = student.SubjectsGrouping?.SubjectsGroupingDetails
                ?.Select(sgd => sgd.SubjectId)
                .ToList() ?? new List<int>();
            
            var examMarks = await _context.ExamMarks
                .Include(em => em.Exam)
                    .ThenInclude(e => e.ExamCategory)
                .Include(em => em.Subject)
                .Where(em => em.StudentId == studentId && em.IsActive)
                .Where(em => studentSubjectIds.Contains(em.SubjectId))
                .OrderByDescending(em => em.ExamDate)
                .ToListAsync();
            
            // Group by subject
            var subjectWiseData = new List<SubjectWiseExamData>();
            foreach (var subjectGroup in examMarks.GroupBy(em => new { em.SubjectId, SubjectName = em.Subject.Name }))
            {
                // Get class average for this subject
                var examIds = subjectGroup.Select(em => em.ExamId).Distinct();
                var classAverages = new Dictionary<int, decimal>();
                
                foreach (var examId in examIds)
                {
                    var classMarks = await _context.ExamMarks
                        .Where(em => em.ExamId == examId 
                            && em.SubjectId == subjectGroup.Key.SubjectId
                            && em.IsActive
                            && _context.Students.Any(s => s.Id == em.StudentId 
                                && s.Class == student.Class 
                                && s.Section == student.Section))
                        .Select(em => em.Percentage)
                        .ToListAsync();
                    
                    classAverages[examId] = classMarks.Any() ? classMarks.Average() : 0;
                }
                
                subjectWiseData.Add(new SubjectWiseExamData
                {
                    SubjectId = subjectGroup.Key.SubjectId,
                    SubjectName = subjectGroup.Key.SubjectName,
                    ExamResults = subjectGroup.Select(em => new ExamResultData
                    {
                        ExamName = em.Exam.Name,
                        CategoryName = em.Exam.ExamCategory.Name,
                        ObtainedMarks = em.ObtainedMarks,
                        TotalMarks = em.TotalMarks,
                        Percentage = em.Percentage,
                        Grade = em.Grade,
                        ExamDate = em.ExamDate
                    }).ToList(),
                    AveragePercentage = subjectGroup.Average(em => em.Percentage),
                    ClassAveragePercentage = classAverages.Values.Any() ? classAverages.Values.Average() : 0
                });
            }
            
            // Group by exam
            var examWiseData = new List<ExamWiseData>();
            foreach (var examGroup in examMarks.GroupBy(em => new { em.ExamId, ExamName = em.Exam.Name, CategoryName = em.Exam.ExamCategory.Name }))
            {
                // Get class average for this exam
                var classExamMarks = await _context.ExamMarks
                    .Where(em => em.ExamId == examGroup.Key.ExamId 
                        && em.IsActive
                        && _context.Students.Any(s => s.Id == em.StudentId 
                            && s.Class == student.Class 
                            && s.Section == student.Section))
                    .GroupBy(em => em.StudentId)
                    .Select(g => new
                    {
                        TotalObtained = g.Sum(em => em.ObtainedMarks),
                        TotalMarks = g.Sum(em => em.TotalMarks)
                    })
                    .ToListAsync();
                
                var classAverage = classExamMarks.Any() && classExamMarks.Sum(x => x.TotalMarks) > 0
                    ? Math.Round((decimal)(classExamMarks.Sum(x => x.TotalObtained) / classExamMarks.Sum(x => x.TotalMarks)) * 100, 2)
                    : 0;
                
                examWiseData.Add(new ExamWiseData
                {
                    ExamId = examGroup.Key.ExamId,
                    ExamName = examGroup.Key.ExamName,
                    CategoryName = examGroup.Key.CategoryName,
                    SubjectResults = examGroup.Select(em => new SubjectResultData
                    {
                        SubjectName = em.Subject.Name,
                        ObtainedMarks = em.ObtainedMarks,
                        TotalMarks = em.TotalMarks,
                        Percentage = em.Percentage,
                        Grade = em.Grade
                    }).ToList(),
                    TotalObtained = examGroup.Sum(em => em.ObtainedMarks),
                    TotalMarks = examGroup.Sum(em => em.TotalMarks),
                    OverallPercentage = examGroup.Sum(em => em.TotalMarks) > 0 
                        ? Math.Round((decimal)(examGroup.Sum(em => em.ObtainedMarks) / examGroup.Sum(em => em.TotalMarks)) * 100, 2) 
                        : 0,
                    ClassAveragePercentage = classAverage
                });
            }
            
            return new ExamAnalysisData
            {
                SubjectWiseData = subjectWiseData,
                ExamWiseData = examWiseData,
                ExamCategories = await _context.ExamCategories
                    .Where(ec => ec.CampusId == student.CampusId && ec.IsActive)
                    .ToListAsync()
            };
        }
        
        private async Task<List<StudentAwardData>> GetStudentAwards(int studentId)
        {
            var awards = await _context.StudentHistories
                .Include(sh => sh.Exam)
                    .ThenInclude(e => e.ExamCategory)
                .Where(sh => sh.StudentId == studentId && sh.IsActive)
                .OrderByDescending(sh => sh.ComputedDate)
                .Select(sh => new StudentAwardData
                {
                    Position = sh.Position,
                    Award = sh.Award,
                    AwardIcon = AwardTypes.GetAward(sh.Position).Icon,
                    AwardColor = AwardTypes.GetAward(sh.Position).Color,
                    FinalPercentage = sh.FinalPercentage,
                    ExamName = sh.Exam.Name,
                    ExamCategoryName = sh.Exam.ExamCategory.Name,
                    AcademicYear = sh.AcademicYear,
                    ComputedDate = sh.ComputedDate
                })
                .ToListAsync();
            
            return awards;
        }
        
        private async Task<List<TimetableSlotData>> GetTimetableForDate(int classId, int sectionId, DateTime date)
        {
            var dayOfWeek = (int)date.DayOfWeek -1;
            if (dayOfWeek == 0) dayOfWeek = 7; // Convert Sunday from 0 to 7
            
            var timetable = await _context.Timetables
                .Include(t => t.TimetableSlots)
                    .ThenInclude(ts => ts.TeacherAssignment)
                        .ThenInclude(ta => ta.Subject)
                .Include(t => t.TimetableSlots)
                    .ThenInclude(ts => ts.TeacherAssignment)
                        .ThenInclude(ta => ta.Teacher)
                .FirstOrDefaultAsync(t => t.ClassId == classId && t.SectionId == sectionId && t.IsActive);
            
            if (timetable == null) return new List<TimetableSlotData>();
            
            var slots = timetable.TimetableSlots
                .Where(ts => ts.DayOfWeek == dayOfWeek)
                .OrderBy(ts => ts.StartTime)
                .Select(ts => new TimetableSlotData
                {
                    PeriodNumber = ts.PeriodNumber,
                    StartTime = ts.StartTime.ToString("hh:mm tt"),
                    EndTime = ts.EndTime.ToString("hh:mm tt"),
                    SubjectName = ts.TeacherAssignment?.Subject?.Name ?? ts.CustomTitle ?? "Break",
                    TeacherName = ts.TeacherAssignment?.Teacher?.FullName ?? "",
                    IsBreak = ts.IsBreak,
                    IsZeroPeriod = ts.IsZeroPeriod
                })
                .ToList();
            
            return slots;
        }
        
        public class StudentDashboardViewModel
        {
            public Student Student { get; set; }
            public string StudentName { get; set; }
            public int StudentId { get; set; }
            public string ClassName { get; set; }
            public string SectionName { get; set; }
            public string ProfilePicture { get; set; }
            
            public List<Attendance> RecentAttendance { get; set; } = new();
            public double AttendancePercentage { get; set; }
            public AttendanceTrendData AttendanceTrends { get; set; }
            
            public List<BillingTransaction> RecentBilling { get; set; } = new();
            public FeeDetailsData FeeDetails { get; set; }
            
            public List<AssignedDuty> AssignedDuties { get; set; } = new();
            public List<ToDoItemData> ToDoList { get; set; } = new();
            public List<HomeworkData> TodayHomework { get; set; } = new();
            public List<TimetableSlotData> TodayTimetable { get; set; } = new();
            public List<TimetableSlotData> TomorrowTimetable { get; set; } = new();
            public List<CalendarEventData> CalendarEvents { get; set; } = new();
            public List<ComplaintData> Complaints { get; set; } = new();
            public ExamAnalysisData ExamAnalysis { get; set; }
            public List<StudentAwardData> StudentAwards { get; set; } = new();
            
            // Teacher of the Month (previous month's winner)
            public TeacherOfMonthInfo? TeacherOfMonth { get; set; }
        }
        
        public class TeacherOfMonthInfo
        {
            public string TeacherName { get; set; }
            public string CampusName { get; set; }
            public decimal Score { get; set; }
            public int Month { get; set; }
            public int Year { get; set; }
        }
        
        public class AttendanceTrendData
        {
            public AttendanceData ThisMonth { get; set; }
            public AttendanceData LastMonth { get; set; }
            public AttendanceData LastYear { get; set; }
        }
        
        public class AttendanceData
        {
            public int Total { get; set; }
            public int Present { get; set; }
            public int Absent { get; set; }
            public double Percentage { get; set; }
        }
        
        public class FeeDetailsData
        {
            public BillingMaster ThisMonthFee { get; set; }
            public BillingMaster LastMonthFee { get; set; }
            public FeeCalculationData FeeCalculation { get; set; }
        }

        public class FeeCalculationData
        {
            public decimal TotalPayable { get; set; }
            public decimal TotalPaid { get; set; }
            public decimal RemainingDues { get; set; }
            public bool HasBillingRecord { get; set; }
            public int? BillingMasterId { get; set; }
            public List<BillingTransaction> Transactions { get; set; } = new();
        }
        
        public class ToDoItemData
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public bool IsCompleted { get; set; }
            public DateTime DueDate { get; set; }
            public string Priority { get; set; }
        }
        
        public class HomeworkData
        {
            public int Id { get; set; }
            public string SubjectName { get; set; }
            public string TeacherName { get; set; }
            public string LessonSummary { get; set; }
            public string Homework { get; set; }
            public string Notes { get; set; }
            public DateTime Date { get; set; }
            public List<string> Images { get; set; } = new();
        }
        
        public class CalendarEventData
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public bool IsHoliday { get; set; }
        }
        
        public class ComplaintData
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string Status { get; set; }
            public DateTime ComplaintDate { get; set; }
            public DateTime? ResolvedDate { get; set; }
        }
        
        public class ExamAnalysisData
        {
            public List<SubjectWiseExamData> SubjectWiseData { get; set; } = new();
            public List<ExamWiseData> ExamWiseData { get; set; } = new();
            public List<ExamCategory> ExamCategories { get; set; } = new();
        }
        
        public class SubjectWiseExamData
        {
            public int SubjectId { get; set; }
            public string SubjectName { get; set; }
            public List<ExamResultData> ExamResults { get; set; } = new();
            public decimal AveragePercentage { get; set; }
            public decimal ClassAveragePercentage { get; set; }
        }
        
        public class ExamResultData
        {
            public string ExamName { get; set; }
            public string CategoryName { get; set; }
            public decimal ObtainedMarks { get; set; }
            public decimal TotalMarks { get; set; }
            public decimal Percentage { get; set; }
            public string Grade { get; set; }
            public DateTime ExamDate { get; set; }
        }
        
        public class ExamWiseData
        {
            public int ExamId { get; set; }
            public string ExamName { get; set; }
            public string CategoryName { get; set; }
            public List<SubjectResultData> SubjectResults { get; set; } = new();
            public decimal TotalObtained { get; set; }
            public decimal TotalMarks { get; set; }
            public decimal OverallPercentage { get; set; }
            public decimal ClassAveragePercentage { get; set; }
        }
        
        public class SubjectResultData
        {
            public string SubjectName { get; set; }
            public decimal ObtainedMarks { get; set; }
            public decimal TotalMarks { get; set; }
            public decimal Percentage { get; set; }
            public string Grade { get; set; }
        }
        
        public class StudentAwardData
        {
            public int Position { get; set; }
            public string Award { get; set; }
            public string AwardIcon { get; set; }
            public string AwardColor { get; set; }
            public decimal FinalPercentage { get; set; }
            public string ExamName { get; set; }
            public string ExamCategoryName { get; set; }
            public int AcademicYear { get; set; }
            public DateTime ComputedDate { get; set; }
        }
        
        public class TimetableSlotData
        {
            public int PeriodNumber { get; set; }
            public string StartTime { get; set; }
            public string EndTime { get; set; }
            public string SubjectName { get; set; }
            public string TeacherName { get; set; }
            public bool IsBreak { get; set; }
            public bool IsZeroPeriod { get; set; }
        }

        // GET: StudentDashboard/WeeklyTimetable
        public async Task<IActionResult> WeeklyTimetable()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .FirstOrDefaultAsync(s => s.Id == currentUser.StudentId);

            if (student == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            // Get the timetable for the student's class and section
            var timetable = await _context.Timetables
                .Include(t => t.Class)
                .Include(t => t.Section)
                .Include(t => t.TimetableSlots)
                    .ThenInclude(ts => ts.TeacherAssignment)
                    .ThenInclude(ta => ta.Teacher)
                .Include(t => t.TimetableSlots)
                    .ThenInclude(ts => ts.TeacherAssignment)
                    .ThenInclude(ta => ta.Subject)
                .FirstOrDefaultAsync(t => t.ClassId == student.Class &&
                                         t.SectionId == student.Section &&
                                         t.IsActive);

            if (timetable == null)
            {
                ViewBag.Message = "No timetable available for your class.";
                return View(new WeeklyTimetableViewModel());
            }

            // Get current week's start date (Monday)
            var today = DateTime.Now.Date;
            var dayOfWeek = (int)today.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7; // Sunday = 7
            var weekStart = today.AddDays(-(dayOfWeek - 1));

            // Get substitutions for the current week
            var substitutions = await _context.Substitutions
                .Include(s => s.SubstituteEmployee)
                .Include(s => s.TimetableSlot)
                .Where(s => s.IsActive &&
                           s.Date >= weekStart &&
                           s.Date < weekStart.AddDays(7) &&
                           timetable.TimetableSlots.Select(ts => ts.Id).Contains(s.TimetableSlotId))
                .ToListAsync();

            // Create a dictionary for quick lookup
            var substitutionDict = substitutions
                .GroupBy(s => new { DayOfWeek = (int)s.Date.DayOfWeek, s.TimetableSlotId })
                .ToDictionary(g => g.Key, g => g.First());

            // Prepare the weekly timetable
            var weeklySlots = new List<WeeklyTimetableSlot>();
            
            for (int day = 1; day <= 5; day++)
            {
                var daySlots = timetable.TimetableSlots
                    .Where(ts => ts.DayOfWeek == day && !ts.IsBreak && !ts.IsZeroPeriod)
                    .OrderBy(ts => ts.PeriodNumber)
                    .ToList();

                foreach (var slot in daySlots)
                {
                    var slotDate = weekStart.AddDays(day - 1);
                    var key = new { DayOfWeek = day, TimetableSlotId = slot.Id };
                    
                    var hasSubstitution = substitutionDict.ContainsKey(key);
                    var substitution = hasSubstitution ? substitutionDict[key] : null;

                    weeklySlots.Add(new WeeklyTimetableSlot
                    {
                        DayOfWeek = day,
                        DayName = GetDayName(day),
                        Date = slotDate,
                        PeriodNumber = slot.PeriodNumber,
                        StartTime = slot.StartTime.ToString("hh:mm tt"),
                        EndTime = slot.EndTime.ToString("hh:mm tt"),
                        SubjectName = slot.TeacherAssignment?.Subject?.Name ?? "N/A",
                        OriginalTeacherName = slot.TeacherAssignment?.Teacher?.FullName ?? "Unassigned",
                        HasSubstitution = hasSubstitution,
                        SubstituteTeacherName = substitution?.SubstituteEmployee?.FullName,
                        SubstituteReason = substitution?.Reason,
                        IsToday = slotDate.Date == today.Date
                    });
                }
            }

            var model = new WeeklyTimetableViewModel
            {
                WeekStart = weekStart,
                WeekEnd = weekStart.AddDays(4),
                ClassName = student.ClassObj?.Name,
                SectionName = student.SectionObj?.Name,
                WeeklySlots = weeklySlots
            };

            return View(model);
        }

        private string GetDayName(int dayOfWeek)
        {
            return dayOfWeek switch
            {
                1 => "Monday",
                2 => "Tuesday",
                3 => "Wednesday",
                4 => "Thursday",
                5 => "Friday",
                _ => ""
            };
        }

        private async Task<TeacherOfMonthInfo?> GetPreviousMonthTopTeacher(int? campusId)
        {
            // Get previous month
            var previousMonth = DateTime.Now.AddMonths(-1);
            var month = previousMonth.Month;
            var year = previousMonth.Year;

            // Get the top performer for previous month
            var topPerformance = await _context.TeacherPerformances
                .Include(tp => tp.Teacher)
                .Include(tp => tp.Campus)
                .Where(tp => tp.Month == month && 
                            tp.Year == year && 
                            tp.IsActive)
                .OrderByDescending(tp => tp.TotalScore)
                .FirstOrDefaultAsync();

            if (topPerformance == null)
                return null;

            // Optionally filter by campus
            if (campusId.HasValue && topPerformance.CampusId != campusId.Value)
            {
                // If you want to show only same-campus teacher, use this
                // Otherwise remove this check to show the overall top teacher
                // return null;
            }

            return new TeacherOfMonthInfo
            {
                TeacherName = topPerformance.Teacher?.FullName ?? "Unknown",
                CampusName = topPerformance.Campus?.Name ?? "Unknown",
                Score = topPerformance.TotalScore,
                Month = month,
                Year = year
            };
        }

        public class WeeklyTimetableViewModel
        {
            public DateTime WeekStart { get; set; }
            public DateTime WeekEnd { get; set; }
            public string ClassName { get; set; }
            public string SectionName { get; set; }
            public List<WeeklyTimetableSlot> WeeklySlots { get; set; } = new();
        }

        public class WeeklyTimetableSlot
        {
            public int DayOfWeek { get; set; }
            public string DayName { get; set; }
            public DateTime Date { get; set; }
            public int PeriodNumber { get; set; }
            public string StartTime { get; set; }
            public string EndTime { get; set; }
            public string SubjectName { get; set; }
            public string OriginalTeacherName { get; set; }
            public bool HasSubstitution { get; set; }
            public string SubstituteTeacherName { get; set; }
            public string SubstituteReason { get; set; }
            public bool IsToday { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendanceData(int studentId, string period)
        {
            try
            {
                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                    return Json(new { success = false, message = "Student not found" });

                var labels = new List<string>();
                var presentData = new List<int>();
                var absentData = new List<int>();
                var leaveData = new List<int>();
                var lateData = new List<int>();

                DateTime startDate, endDate;
                var currentYear = DateTime.Now.Year;
                var currentMonth = DateTime.Now.Month;
                
                if (period == "thisYear")
                {
                    startDate = new DateTime(currentYear, 1, 1);
                    endDate = DateTime.Today;
                    
                    // Monthly data for this year
                    for (int month = 1; month <= currentMonth; month++)
                    {
                        labels.Add(new DateTime(currentYear, month, 1).ToString("MMM"));
                        
                        var monthStart = new DateTime(currentYear, month, 1);
                        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                        
                        // Get holidays and sundays for this month
                        var monthHolidays = await _context.CalendarEvents
                            .Where(e => e.CampusId == student.CampusId 
                                && e.IsHoliday 
                                && e.StartDate >= monthStart 
                                && e.StartDate <= monthEnd)
                            .Select(e => e.StartDate.Date)
                            .ToListAsync();
                        
                        var monthSundays = new List<DateTime>();
                        for (var date = monthStart; date <= monthEnd; date = date.AddDays(1))
                        {
                            if (date.DayOfWeek == DayOfWeek.Sunday)
                                monthSundays.Add(date.Date);
                        }
                        
                        // Get attendance records
                        var monthRecords = await _context.Attendance
                            .Where(a => a.StudentId == studentId 
                                && a.Date >= monthStart 
                                && a.Date <= monthEnd 
                                && a.Date >= student.RegistrationDate)
                            .ToListAsync();
                        
                        // Calculate working days
                        var daysInMonth = (monthEnd - monthStart).Days + 1;
                        var workingDays = daysInMonth - monthSundays.Count - monthHolidays.Count;
                        
                        // Count status
                        var present = monthRecords.Count(a => a.Status == "P");
                        var absent = monthRecords.Count(a => a.Status == "A");
                        var leave = monthRecords.Count(a => a.Status == "Leave");
                        var late = monthRecords.Count(a => a.Status == "L");
                        
                        // If no record exists for a working day, count as present
                        var recordedDays = monthRecords.Count;
                        if (recordedDays < workingDays)
                        {
                            present += (workingDays - recordedDays);
                        }
                        
                        presentData.Add(present);
                        absentData.Add(absent);
                        leaveData.Add(leave);
                        lateData.Add(late);
                    }
                }
                else // lastYear
                {
                    startDate = new DateTime(currentYear - 1, 1, 1);
                    endDate = new DateTime(currentYear - 1, 12, 31);
                    
                    // Monthly data for last year
                    for (int month = 1; month <= 12; month++)
                    {
                        labels.Add(new DateTime(currentYear - 1, month, 1).ToString("MMM"));
                        
                        var monthStart = new DateTime(currentYear - 1, month, 1);
                        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                        
                        // Get holidays and sundays for this month
                        var monthHolidays = await _context.CalendarEvents
                            .Where(e => e.CampusId == student.CampusId 
                                && e.IsHoliday 
                                && e.StartDate >= monthStart 
                                && e.StartDate <= monthEnd)
                            .Select(e => e.StartDate.Date)
                            .ToListAsync();
                        
                        var monthSundays = new List<DateTime>();
                        for (var date = monthStart; date <= monthEnd; date = date.AddDays(1))
                        {
                            if (date.DayOfWeek == DayOfWeek.Sunday)
                                monthSundays.Add(date.Date);
                        }
                        
                        // Get attendance records
                        var monthRecords = await _context.Attendance
                            .Where(a => a.StudentId == studentId 
                                && a.Date >= monthStart 
                                && a.Date <= monthEnd 
                                && a.Date >= student.RegistrationDate)
                            .ToListAsync();
                        
                        // Calculate working days
                        var daysInMonth = (monthEnd - monthStart).Days + 1;
                        var workingDays = daysInMonth - monthSundays.Count - monthHolidays.Count;
                        
                        // Count status
                        var present = monthRecords.Count(a => a.Status == "P");
                        var absent = monthRecords.Count(a => a.Status == "A");
                        var leave = monthRecords.Count(a => a.Status == "Leave");
                        var late = monthRecords.Count(a => a.Status == "L");
                        
                        // If no record exists for a working day, count as present
                        var recordedDays = monthRecords.Count;
                        if (recordedDays < workingDays)
                        {
                            present += (workingDays - recordedDays);
                        }
                        
                        presentData.Add(present);
                        absentData.Add(absent);
                        leaveData.Add(leave);
                        lateData.Add(late);
                    }
                }
                
                var totalDays = presentData.Sum() + absentData.Sum() + leaveData.Sum() + lateData.Sum();
                
                return Json(new
                {
                    success = true,
                    labels,
                    presentData,
                    absentData,
                    leaveData,
                    lateData,
                    totals = new
                    {
                        total = totalDays,
                        present = presentData.Sum(),
                        absent = absentData.Sum(),
                        leave = leaveData.Sum(),
                        late = lateData.Sum()
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
