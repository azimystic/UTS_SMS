using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using UTS_SMS.ViewModels;

namespace UTS_SMS.Controllers
{
    // Teacher Dashboard Controller
    [Authorize(Roles = "Admin,Teacher")]
    public class TeacherController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeacherController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var teacher = await _context.Employees
                .Include(e => e.Campus)
                .FirstOrDefaultAsync(e => e.Id == currentUser.EmployeeId);

            if (teacher == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            // Get teacher's performance for current month
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            var teacherPerformance = await _context.TeacherPerformances
                .Where(tp => tp.TeacherId == teacher.Id && 
                            tp.Month == currentMonth && 
                            tp.Year == currentYear && 
                            tp.IsActive)
                .FirstOrDefaultAsync();

            var model = new TeacherDashboardViewModel
            {
                Teacher = teacher,
                TeacherName = teacher.FullName,
                TeacherId = teacher.Id,
                ProfilePicture = teacher.ProfilePicture,
                CampusName = teacher.Campus?.Name ?? "N/A",
                
                // Attendance data (teacher's own)
                AttendancePercentage = (decimal)await GetTeacherAttendancePercentage(teacher.Id),
                RecentAttendance = await GetTeacherRecentAttendance(teacher.Id),
                
                // Leave Balance
                LeaveBalance = await GetLeaveBalance(teacher.Id),
                
                // Payroll data - Last Month and Month Before Last
                PayrollDetails = await GetPayrollDetails(teacher.Id),
                
                // Exam results analysis
                ExamAnalysis = await GetExamAnalysis(teacher.Id),
                
                // Timetable - Today and Tomorrow
                TodayTimetable = await GetTimetableForDay(teacher.Id, DateTime.Today),
                TomorrowTimetable = await GetTimetableForDay(teacher.Id, DateTime.Today.AddDays(1)),
                
                // Calendar events (including exam date sheets)
                CalendarEvents = await GetCalendarEvents(teacher.CampusId, teacher.Id),
                
                // Diary entries
                TodayDiaries = await GetTodayDiaries(teacher.Id),
                
                // Assigned duties
                AssignedDuties = await GetAssignedDuties(teacher.Id),
                
                // Teacher complaints (open complaints about teacher from students)
                TeacherComplaints = await GetTeacherComplaints(teacher.Id),
                
                // To-do list
                ToDoList = await GetToDoList(teacher.Id),
                
                // Teacher of the Month (if this teacher is the winner for previous month)
                TeacherOfMonth = await GetTeacherOfMonth(teacher.Id),
                
                // Previous Month's Top Teacher (for all teachers to see)
                PreviousMonthTopTeacher = await GetPreviousMonthTopTeacher(),
                
                // All awards for this teacher
                AllTeacherAwards = await GetAllTeacherAwards(teacher.Id),
                
                // Performance
                CurrentPerformance = teacherPerformance,
                
                // Basic counts
                TotalStudents = await _context.Students.CountAsync(s => !s.HasLeft && s.CampusId == teacher.CampusId),
                StudentsToday = await _context.Attendance
                    .CountAsync(a => a.Date.Date == DateTime.Today && a.Status == "P" && a.Student.CampusId == teacher.CampusId)
            };

            return View(model);
        }

        private async Task<double> GetTeacherAttendancePercentage(int teacherId)
        {
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var attendanceRecords = await _context.EmployeeAttendance
                .Where(ea => ea.EmployeeId == teacherId &&
                            ea.Date.Month == currentMonth &&
                            ea.Date.Year == currentYear)
                .ToListAsync();

            if (!attendanceRecords.Any())
                return 0;

            var presentCount = attendanceRecords.Count(ea => ea.Status == "P");
            return Math.Round((double)presentCount / attendanceRecords.Count * 100, 2);
        }

        private async Task<List<EmployeeAttendance>> GetTeacherRecentAttendance(int teacherId)
        {
            return await _context.EmployeeAttendance
                .Where(ea => ea.EmployeeId == teacherId)
                .OrderByDescending(ea => ea.Date)
                .Take(30)
                .ToListAsync();
        }

        private async Task<TeacherPayrollSummary> GetPayrollDetails(int teacherId)
        {
            var thisMonth = DateTime.Now;
            var lastMonth = thisMonth.AddMonths(-1);
            var monthBeforeLast = thisMonth.AddMonths(-2);

            // Get salary definition if no payroll exists
            var salaryDef = await _context.SalaryDefinitions
                .Where(s => s.EmployeeId == teacherId && s.IsActive)
                .OrderByDescending(s => s.CreatedDate)
                .FirstOrDefaultAsync();

            var lastMonthPayroll = await _context.PayrollMasters
                .Include(p => p.Transactions)
                .Where(p => p.EmployeeId == teacherId && 
                           p.ForMonth == lastMonth.Month && 
                           p.ForYear == lastMonth.Year)
                .FirstOrDefaultAsync();

            var monthBeforeLastPayroll = await _context.PayrollMasters
                .Include(p => p.Transactions)
                .Where(p => p.EmployeeId == teacherId && 
                           p.ForMonth == monthBeforeLast.Month && 
                           p.ForYear == monthBeforeLast.Year)
                .FirstOrDefaultAsync();

            return new TeacherPayrollSummary
            {
                LastMonthGross = lastMonthPayroll?.GrossSalary ?? salaryDef?.GrossSalary ?? 0,
                LastMonthNet = lastMonthPayroll?.NetSalary ?? salaryDef?.NetSalary ?? 0,
                LastMonthDeductions = lastMonthPayroll != null ? (lastMonthPayroll.Deductions + lastMonthPayroll.AttendanceDeduction) : salaryDef?.TotalDeductions ?? 0,
                LastMonthBasic = lastMonthPayroll?.BasicSalary ?? salaryDef?.BasicSalary ?? 0,
                LastMonthAllowances = lastMonthPayroll?.Allowances ?? salaryDef?.TotalAllowances ?? 0,
                LastMonthBonus = lastMonthPayroll?.Bonus ?? 0,
                LastMonthPreviousBalance = lastMonthPayroll?.PreviousBalance ?? 0,
                LastMonthAttendanceDeduction = lastMonthPayroll?.AttendanceDeduction ?? 0,
                
                MonthBeforeLastGross = monthBeforeLastPayroll?.GrossSalary ?? salaryDef?.GrossSalary ?? 0,
                MonthBeforeLastNet = monthBeforeLastPayroll?.NetSalary ?? salaryDef?.NetSalary ?? 0,
                MonthBeforeLastDeductions = monthBeforeLastPayroll != null ? (monthBeforeLastPayroll.Deductions + monthBeforeLastPayroll.AttendanceDeduction) : salaryDef?.TotalDeductions ?? 0,
                MonthBeforeLastBasic = monthBeforeLastPayroll?.BasicSalary ?? salaryDef?.BasicSalary ?? 0,
                MonthBeforeLastAllowances = monthBeforeLastPayroll?.Allowances ?? salaryDef?.TotalAllowances ?? 0,
                MonthBeforeLastBonus = monthBeforeLastPayroll?.Bonus ?? 0,
                MonthBeforeLastPreviousBalance = monthBeforeLastPayroll?.PreviousBalance ?? 0,
                MonthBeforeLastAttendanceDeduction = monthBeforeLastPayroll?.AttendanceDeduction ?? 0,
                
                LastMonthRecord = lastMonthPayroll,
                MonthBeforeLastRecord = monthBeforeLastPayroll,
                
                LastMonthMonth = lastMonth.Month,
                LastMonthYear = lastMonth.Year,
                MonthBeforeLastMonth = monthBeforeLast.Month,
                MonthBeforeLastYear = monthBeforeLast.Year
            };
        }

        private async Task<TeacherExamAnalysis> GetExamAnalysis(int teacherId)
        {
            var teacherAssignments = await _context.TeacherAssignments
                .Where(ta => ta.TeacherId == teacherId && ta.IsActive)
                .Include(ta => ta.Class)
                .Include(ta => ta.Section)
                .Include(ta => ta.Subject)
                .ToListAsync();

            var classWiseData = new List<ClassWiseResult>();
            var testWiseData = new List<TestWiseResult>();

            foreach (var assignment in teacherAssignments)
            {
                int? academicYear = null;
                
                if (!string.IsNullOrEmpty(assignment.Class.CurrentAcademicYear))
                {
                    var yearParts = assignment.Class.CurrentAcademicYear.Split('-');
                    if (yearParts.Length > 0 && int.TryParse(yearParts[0], out int parsedYear))
                    {
                        academicYear = parsedYear;
                    }
                }
                
                if (!academicYear.HasValue)
                {
                    var currentAcademicYear = await _context.AcademicYear
                        .OrderByDescending(ay => ay.Year)
                        .FirstOrDefaultAsync();
                    academicYear = currentAcademicYear?.Year ?? DateTime.Now.Year;
                }

                var students = await _context.Students
                    .Where(s => s.Class == assignment.ClassId && 
                               s.Section == assignment.SectionId && 
                               !s.HasLeft)
                    .ToListAsync();

                if (students.Any())
                {
                    var studentIds = students.Select(s => s.Id).ToList();

                    var examMarksQuery = _context.ExamMarks
                        .Where(em => studentIds.Contains(em.StudentId) &&
                                     em.SubjectId == assignment.SubjectId &&
                                     em.AcademicYear == academicYear.Value);

                    var examMarks = await examMarksQuery
                        .Include(em => em.Exam)
                        .ThenInclude(e => e.ExamCategory)
                        .ToListAsync();

                    if (examMarks.Any())
                    {
                        var avgPercentage = examMarks.Average(em => 
                            em.TotalMarks > 0 ? (double)em.ObtainedMarks / (double)em.TotalMarks * 100 : 0);

                        classWiseData.Add(new ClassWiseResult
                        {
                            ClassName = $"{assignment.Class.Name} - {assignment.Section.Name}",
                            SubjectName = assignment.Subject.Name,
                            SubjectId = assignment.SubjectId,
                            ClassId = assignment.ClassId,
                            SectionId = assignment.SectionId,
                            AveragePercentage = Math.Round(avgPercentage, 2),
                            TotalStudents = students.Count,
                            TotalTests = examMarks.Select(em => em.ExamId).Distinct().Count()
                        });

                        var testGroups = examMarks.GroupBy(em => em.ExamId);
                        foreach (var testGroup in testGroups)
                        {
                            var exam = testGroup.First().Exam;
                            var testAvg = testGroup.Average(em => 
                                em.TotalMarks > 0 ? (double)em.ObtainedMarks / (double)em.TotalMarks * 100 : 0);

                            testWiseData.Add(new TestWiseResult
                            {
                                ExamId = exam.Id,
                                ExamName = exam.Name,
                                CategoryName = exam.ExamCategory?.Name ?? "N/A",
                                CategoryId = exam.ExamCategoryId,
                                SubjectName = assignment.Subject.Name,
                                SubjectId = assignment.SubjectId,
                                ClassName = $"{assignment.Class.Name} - {assignment.Section.Name}",
                                AveragePercentage = Math.Round(testAvg, 2),
                                TotalStudents = testGroup.Count(),
                                ExamDate = testGroup.First().ExamDate
                            });
                        }
                    }
                }
            }

            return new TeacherExamAnalysis
            {
                ClassWiseResults = classWiseData,
                TestWiseResults = testWiseData
            };
        }

        private async Task<List<CalendarEvent>> GetCalendarEvents(int? campusId, int teacherId)
        {
            var today = DateTime.Today;
            var nextMonth = today.AddMonths(1);

            // Get regular calendar events
            var events = await _context.CalendarEvents
                .Where(e => e.CampusId == campusId && 
                           e.IsActive &&
                           e.StartDate <= nextMonth &&
                           (e.EndDate == null || e.EndDate >= today))
                .OrderBy(e => e.StartDate)
                .ToListAsync();

            var calendarEvents = new List<CalendarEvent>();
            
            foreach (var e in events)
            {
                // For multi-day events, create entries for each day
                var startDate = e.StartDate > today ? e.StartDate : today;
                var endDate = e.EndDate ?? e.StartDate;
                
                for (var date = startDate; date <= endDate && date <= nextMonth; date = date.AddDays(1))
                {
                    calendarEvents.Add(new CalendarEvent
                    {
                        Id = e.Id,
                        Title = e.EventName,
                        Description = e.Description,
                        StartDate = date,
                        EndDate = e.EndDate,
                        IsHoliday = e.IsHoliday,
                        IsMultiDay = e.EndDate.HasValue && e.EndDate.Value.Date != e.StartDate.Date,
                        EventType = "holiday"
                    });
                }
            }

            // Get teacher's exam date sheets
            var teacherAssignments = await _context.TeacherAssignments
                .Where(ta => ta.TeacherId == teacherId && ta.IsActive)
                .Select(ta => new { ta.ClassId, ta.SectionId, ta.SubjectId })
                .ToListAsync();

            foreach (var assignment in teacherAssignments)
            {
                var examSheets = await _context.ExamDateSheets
                    .Include(eds => eds.Subject)
                    .Include(eds => eds.Exam)
                    .ThenInclude(e => e.ExamCategory)
                    .Include(eds => eds.ClassSections)
                    .Where(eds => eds.SubjectId == assignment.SubjectId &&
                                 eds.ExamDate >= today &&
                                 eds.ExamDate <= nextMonth &&
                                 eds.IsActive &&
                                 eds.ClassSections.Any(cs => cs.ClassId == assignment.ClassId && cs.SectionId == assignment.SectionId))
                    .ToListAsync();

                foreach (var sheet in examSheets)
                {
                    var classSection = sheet.ClassSections.FirstOrDefault(cs => cs.ClassId == assignment.ClassId && cs.SectionId == assignment.SectionId);
                    
                    calendarEvents.Add(new CalendarEvent
                    {
                        Id = sheet.Id,
                        Title = $"{sheet.Subject?.Name ?? "Exam"} - {sheet.Exam?.Name ?? ""}",
                        Description = $"{sheet.ExamCategory?.Name ?? ""} | {classSection?.Class?.Name ?? ""}-{classSection?.Section?.Name ?? ""}",
                        StartDate = sheet.ExamDate,
                        EndDate = sheet.ExamDate,
                        IsHoliday = false,
                        IsMultiDay = false,
                        EventType = "exam"
                    });
                }
            }

            return calendarEvents.OrderBy(e => e.StartDate).ToList();
        }

        [HttpGet]
        public async Task<IActionResult> GetCalendarEventsByMonth(int year, int month)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var teacher = await _context.Employees.FindAsync(currentUser.EmployeeId);
                
                if (teacher == null)
                    return Json(new { success = false, message = "Teacher not found" });

                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                // Get regular events
                var events = await _context.CalendarEvents
                    .Where(e => e.CampusId == teacher.CampusId &&
                               e.IsActive &&
                               ((e.StartDate >= startDate && e.StartDate <= endDate) ||
                                (e.EndDate.HasValue && e.EndDate.Value >= startDate && e.StartDate <= endDate)))
                    .OrderBy(e => e.StartDate)
                    .Select(e => new
                    {
                        id = e.Id,
                        title = e.EventName,
                        description = e.Description,
                        startDate = e.StartDate,
                        endDate = e.EndDate,
                        isHoliday = e.IsHoliday,
                        isMultiDay = e.EndDate.HasValue && e.EndDate.Value.Date != e.StartDate.Date,
                        eventType = "holiday"
                    })
                    .ToListAsync();

                // Get ALL exam date sheets for the campus (not just teacher's subjects)
                var examSheets = await _context.ExamDateSheets
                    .Where(eds => eds.CampusId == teacher.CampusId &&
                                 eds.ExamDate >= startDate &&
                                 eds.ExamDate <= endDate &&
                                 eds.IsActive)
                    .Include(eds => eds.Subject)
                    .Include(eds => eds.Exam)
                    .ThenInclude(e => e.ExamCategory)
                    .Include(eds => eds.ClassSections)
                    .ThenInclude(cs => cs.Class)
                    .Include(eds => eds.ClassSections)
                    .ThenInclude(cs => cs.Section)
                    .OrderBy(eds => eds.Subject.Name)
                    .ToListAsync();

                // Group exam sheets by exam and subject
                var groupedExamSheets = examSheets
                    .GroupBy(eds => new { eds.ExamDate, eds.ExamId, eds.SubjectId, ExamName = eds.Exam.Name, SubjectName = eds.Subject.Name })
                    .Select(g => new
                    {
                        id = g.First().Id,
                        examName = g.Key.ExamName,
                        subjectName = g.Key.SubjectName,
                        title = $"{g.Key.ExamName} - {g.Key.SubjectName}",
                        description = string.Join(", ", g
                            .SelectMany(eds => eds.ClassSections)
                            .Where(cs => cs.IsActive && cs.Class != null && cs.Section != null)
                            .Select(cs => $"{cs.Class.Name}-{cs.Section.Name}")
                            .Distinct()
                            .OrderBy(x => x)),
                        startDate = g.Key.ExamDate,
                        endDate = (DateTime?)g.Key.ExamDate,
                        isHoliday = false,
                        isMultiDay = false,
                        eventType = "exam",
                        classes = g.SelectMany(eds => eds.ClassSections)
                            .Where(cs => cs.IsActive && cs.Class != null && cs.Section != null)
                            .Select(cs => new { className = cs.Class.Name, sectionName = cs.Section.Name })
                            .Distinct()
                            .OrderBy(c => c.className)
                            .ToList()
                    })
                    .ToList();

                var allEvents = events.Cast<object>().Concat(groupedExamSheets).ToList();

                return Json(new { success = true, events = allEvents });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetClassWiseData(int teacherId, int? subjectId = null)
        {
            try
            {
                var teacherAssignments = await _context.TeacherAssignments
                    .Where(ta => ta.TeacherId == teacherId && ta.IsActive)
                    .Where(ta => !subjectId.HasValue || ta.SubjectId == subjectId.Value)
                    .Include(ta => ta.Class)
                    .Include(ta => ta.Section)
                    .Include(ta => ta.Subject)
                    .ToListAsync();

                var classWiseData = new List<object>();

                foreach (var assignment in teacherAssignments)
                {
                    int? academicYear = null;
                    
                    if (!string.IsNullOrEmpty(assignment.Class.CurrentAcademicYear))
                    {
                        var yearParts = assignment.Class.CurrentAcademicYear.Split('-');
                        if (yearParts.Length > 0 && int.TryParse(yearParts[0], out int parsedYear))
                        {
                            academicYear = parsedYear;
                        }
                    }
                    
                    if (!academicYear.HasValue)
                    {
                        var currentAcademicYear = await _context.AcademicYear
                            .OrderByDescending(ay => ay.Year)
                            .FirstOrDefaultAsync();
                        academicYear = currentAcademicYear?.Year ?? DateTime.Now.Year;
                    }

                    var students = await _context.Students
                        .Where(s => s.Class == assignment.ClassId && 
                                   s.Section == assignment.SectionId && 
                                   !s.HasLeft)
                        .ToListAsync();

                    if (students.Any())
                    {
                        var studentIds = students.Select(s => s.Id).ToList();

                        var examMarks = await _context.ExamMarks
                            .Where(em => studentIds.Contains(em.StudentId) &&
                                         em.SubjectId == assignment.SubjectId &&
                                         em.AcademicYear == academicYear.Value)
                            .ToListAsync();

                        if (examMarks.Any())
                        {
                            var avgPercentage = examMarks.Average(em => 
                                em.TotalMarks > 0 ? (double)em.ObtainedMarks / (double)em.TotalMarks * 100 : 0);

                            classWiseData.Add(new
                            {
                                className = $"{assignment.Class.Name} - {assignment.Section.Name}",
                                subjectName = assignment.Subject.Name,
                                averagePercentage = Math.Round(avgPercentage, 2),
                                totalStudents = students.Count,
                                totalTests = examMarks.Select(em => em.ExamId).Distinct().Count()
                            });
                        }
                    }
                }

                return Json(new { success = true, data = classWiseData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTestWiseData(int teacherId, int? subjectId = null, int? categoryId = null)
        {
            try
            {
                var teacherAssignments = await _context.TeacherAssignments
                    .Where(ta => ta.TeacherId == teacherId && ta.IsActive)
                    .Where(ta => !subjectId.HasValue || ta.SubjectId == subjectId.Value)
                    .Include(ta => ta.Class)
                    .Include(ta => ta.Section)
                    .Include(ta => ta.Subject)
                    .ToListAsync();

                var testWiseData = new List<object>();

                foreach (var assignment in teacherAssignments)
                {
                    int? academicYear = null;
                    
                    if (!string.IsNullOrEmpty(assignment.Class.CurrentAcademicYear))
                    {
                        var yearParts = assignment.Class.CurrentAcademicYear.Split('-');
                        if (yearParts.Length > 0 && int.TryParse(yearParts[0], out int parsedYear))
                        {
                            academicYear = parsedYear;
                        }
                    }
                    
                    if (!academicYear.HasValue)
                    {
                        var currentAcademicYear = await _context.AcademicYear
                            .OrderByDescending(ay => ay.Year)
                            .FirstOrDefaultAsync();
                        academicYear = currentAcademicYear?.Year ?? DateTime.Now.Year;
                    }

                    var students = await _context.Students
                        .Where(s => s.Class == assignment.ClassId && 
                                   s.Section == assignment.SectionId && 
                                   !s.HasLeft)
                        .ToListAsync();

                    if (students.Any())
                    {
                        var studentIds = students.Select(s => s.Id).ToList();

                        var examMarksQuery = _context.ExamMarks
                            .Where(em => studentIds.Contains(em.StudentId) &&
                                         em.SubjectId == assignment.SubjectId &&
                                         em.AcademicYear == academicYear.Value);

                        if (categoryId.HasValue)
                        {
                            examMarksQuery = examMarksQuery.Where(em => em.Exam.ExamCategoryId == categoryId.Value);
                        }

                        var examMarks = await examMarksQuery
                            .Include(em => em.Exam)
                            .ThenInclude(e => e.ExamCategory)
                            .ToListAsync();

                        var testGroups = examMarks.GroupBy(em => em.ExamId);
                        foreach (var testGroup in testGroups)
                        {
                            var exam = testGroup.First().Exam;
                            var testAvg = testGroup.Average(em => 
                                em.TotalMarks > 0 ? (double)em.ObtainedMarks / (double)em.TotalMarks * 100 : 0);

                            testWiseData.Add(new
                            {
                                examName = exam.Name,
                                categoryName = exam.ExamCategory?.Name ?? "N/A",
                                subjectName = assignment.Subject.Name,
                                className = $"{assignment.Class.Name} - {assignment.Section.Name}",
                                averagePercentage = Math.Round(testAvg, 2),
                                totalStudents = testGroup.Count()
                            });
                        }
                    }
                }

                return Json(new { success = true, data = testWiseData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTeacherSubjects(int teacherId)
        {
            try
            {
                var subjects = await _context.TeacherAssignments
                    .Where(ta => ta.TeacherId == teacherId && ta.IsActive)
                    .Include(ta => ta.Subject)
                    .Select(ta => new { id = ta.SubjectId, name = ta.Subject.Name })
                    .Distinct()
                    .OrderBy(s => s.name)
                    .ToListAsync();

                return Json(new { success = true, subjects });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetExamCategories(int teacherId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var teacher = await _context.Employees.FindAsync(currentUser.EmployeeId);

                var categories = await _context.ExamCategories
                    .Where(ec => ec.CampusId == teacher.CampusId && ec.IsActive)
                    .Select(ec => new { id = ec.Id, name = ec.Name })
                    .OrderBy(ec => ec.name)
                    .ToListAsync();

                return Json(new { success = true, categories });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<List<TimetableEntry>> GetTimetableForDay(int teacherId, DateTime date)
        {
            var dayOfWeek = (int)date.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7;

            var assignments = await _context.TeacherAssignments
                .Where(ta => ta.TeacherId == teacherId && ta.IsActive)
                .Include(ta => ta.Class)
                .Include(ta => ta.Section)
                .Include(ta => ta.Subject)
                .ToListAsync();

            var timetableEntries = new List<TimetableEntry>();

            foreach (var assignment in assignments)
            {
                var slots = await _context.TimetableSlots
                    .Where(ts => ts.TeacherAssignmentId == assignment.Id &&
                                ts.DayOfWeek == dayOfWeek &&
                                !ts.IsBreak &&
                                ts.Timetable.IsActive)
                    .Include(ts => ts.Timetable)
                    .OrderBy(ts => ts.StartTime)
                    .ToListAsync();

                foreach (var slot in slots)
                {
                    var substitution = await _context.Substitutions
                        .Include(s => s.SubstituteEmployee)
                        .FirstOrDefaultAsync(s => s.TimetableSlotId == slot.Id &&
                                                 s.Date.Date == date.Date &&
                                                 s.IsActive);

                    timetableEntries.Add(new TimetableEntry
                    {
                        Id = slot.Id,
                        ClassName = assignment.Class.Name,
                        SectionName = assignment.Section.Name,
                        SubjectName = assignment.Subject.Name,
                        StartTime = slot.StartTime.TimeOfDay,
                        EndTime = slot.EndTime.TimeOfDay,
                        DayOfWeek = date.DayOfWeek.ToString(),
                        SubstitutionInfo = substitution != null ? 
                            $"Substitute: {substitution.SubstituteEmployee.FullName}" : null
                    });
                }
            }

            return timetableEntries.OrderBy(t => t.StartTime).ToList();
        }

        private async Task<List<DiaryInfo>> GetTodayDiaries(int teacherId)
        {
            var today = DateTime.Today;
            
            var diaries = await _context.Diaries
                .Where(d => d.TeacherAssignment.TeacherId == teacherId && d.Date.Date == today)
                .Include(d => d.TeacherAssignment)
                .ThenInclude(ta => ta.Class)
                .Include(d => d.TeacherAssignment)
                .ThenInclude(ta => ta.Section)
                .Include(d => d.TeacherAssignment)
                .ThenInclude(ta => ta.Subject)
                .Include(d => d.TeacherAssignment)
                .ThenInclude(ta => ta.Teacher)
                .OrderBy(d => d.CreatedAt)
                .ToListAsync();

            return diaries.Select(d => new DiaryInfo
            {
                Id = d.Id,
                ClassName = d.TeacherAssignment.Class.Name,
                SectionName = d.TeacherAssignment.Section.Name,
                SubjectName = d.TeacherAssignment.Subject.Name,
                TeacherName = d.TeacherAssignment.Teacher.FullName,
                Homework = d.HomeworkGiven ?? "",
                Date = d.Date,
                PendingCount = 0
            }).ToList();
        }

        private async Task<List<AssignedDutyInfo>> GetAssignedDuties(int teacherId)
        {
            var duties = await _context.AssignedDuties
                .Where(d => d.EmployeeId == teacherId && 
                           d.IsActive &&
                           d.Status != "Completed" &&
                           d.Status != "Cancelled")
                .OrderByDescending(d => d.Priority == "High" ? 3 : d.Priority == "Medium" ? 2 : 1)
                .ThenBy(d => d.DueDate)
                .Take(10)
                .ToListAsync();

            return duties.Select(d => new AssignedDutyInfo
            {
                Id = d.Id,
                DutyTitle = d.DutyTitle,
                Description = d.Description ?? "",
                StartDate = d.StartDate,
                DueDate = d.DueDate,
                Priority = d.Priority,
                Status = d.Status
            }).ToList();
        }

        private async Task<List<ComplaintInfo>> GetTeacherComplaints(int teacherId)
        {
            var complaints = await _context.StudentComplaints
                .Where(c => c.TeacherId == teacherId &&
                           c.ReporterType == "Student" &&
                           c.Status == "Open" &&
                           c.IsActive)
                .Include(c => c.Student)
                .ThenInclude(s => s.ClassObj)
                .Include(c => c.Student)
                .ThenInclude(s => s.SectionObj)
                .OrderByDescending(c => c.ComplaintDate)
                .Take(10)
                .ToListAsync();

            return complaints.Select(c => new ComplaintInfo
            {
                Id = c.Id,
                Title = c.ComplaintTitle,
                Description = c.ComplaintDescription,
                StudentName = c.Student.StudentName,
                ClassName = c.Student.ClassObj?.Name ?? "",
                SectionName = c.Student.SectionObj?.Name ?? "",
                Priority = c.Priority,
                Status = c.Status,
                ComplaintDate = c.ComplaintDate,
                ComplaintType = c.ComplaintType
            }).ToList();
        }

        private async Task<List<ToDoItem>> GetToDoList(int teacherId)
        {
            var currentUser = await _userManager.Users
                .FirstOrDefaultAsync(u => u.EmployeeId == teacherId);

            if (currentUser == null)
                return new List<ToDoItem>();

            var todos = await _context.ToDos
                .Where(t => t.UserId == currentUser.Id && t.IsActive)
                .OrderBy(t => t.IsCompleted)
                .ThenBy(t => t.DueDate)
                .ToListAsync();

            return todos.Select(t => new ToDoItem
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description ?? "",
                IsCompleted = t.IsCompleted,
                DueDate = t.DueDate,
                Priority = t.Priority
            }).ToList();
        }

        private async Task<LeaveBalanceInfo?> GetLeaveBalance(int teacherId)
        {
            var currentYear = DateTime.Now.Year;
            
            var leaveBalances = await _context.LeaveBalances
                .Where(lb => lb.EmployeeId == teacherId && 
                            lb.Year == currentYear)
                .ToListAsync();

            if (!leaveBalances.Any())
                return null;

            var casualBalance = leaveBalances.FirstOrDefault(lb => lb.LeaveType.Contains("Casual"));
            var sickBalance = leaveBalances.FirstOrDefault(lb => lb.LeaveType.Contains("Sick"));
            var annualBalance = leaveBalances.FirstOrDefault(lb => lb.LeaveType.Contains("Annual"));

            return new LeaveBalanceInfo
            {
                CasualTotal = casualBalance != null ? (int)casualBalance.TotalAllocated : 0,
                CasualUsed = casualBalance != null ? (int)casualBalance.Used : 0,
                CasualRemaining = casualBalance != null ? (int)casualBalance.Available : 0,
                SickTotal = sickBalance != null ? (int)sickBalance.TotalAllocated : 0,
                SickUsed = sickBalance != null ? (int)sickBalance.Used : 0,
                SickRemaining = sickBalance != null ? (int)sickBalance.Available : 0,
                AnnualTotal = annualBalance != null ? (int)annualBalance.TotalAllocated : 0,
                AnnualUsed = annualBalance != null ? (int)annualBalance.Used : 0,
                AnnualRemaining = annualBalance != null ? (int)annualBalance.Available : 0
            };
        }

        private async Task<TeacherOfMonthInfo?> GetTeacherOfMonth(int teacherId)
        {
            var lastMonth = DateTime.Now.AddMonths(-1);
            
            var award = await _context.TeacherPerformances
                .Include(tp => tp.Teacher)
                .ThenInclude(t => t.Campus)
                .Where(tp => tp.TeacherId == teacherId &&
                            tp.Month == lastMonth.Month &&
                            tp.Year == lastMonth.Year)
                .OrderByDescending(tp => tp.TotalScore)
                .FirstOrDefaultAsync();

            if (award == null || award.TotalScore < 16)
                return null;

            return new TeacherOfMonthInfo
            {
                Month = award.Month,
                Year = award.Year,
                Score = award.TotalScore,
                Achievements = "",
                TeacherName = award.Teacher.FullName,
                CampusName = award.Teacher.Campus?.Name ?? ""
            };
        }

        private async Task<List<TeacherOfMonthInfo>> GetAllTeacherAwards(int teacherId)
        {
            var awards = await _context.TeacherPerformances
                .Where(tp => tp.TeacherId == teacherId && tp.TotalScore >= 16)
                .OrderByDescending(tp => tp.Year)
                .ThenByDescending(tp => tp.Month)
                .Take(10)
                .ToListAsync();

            return awards.Select(a => new TeacherOfMonthInfo
            {
                Month = a.Month,
                Year = a.Year,
                Score = a.TotalScore,
                Achievements = ""
            }).ToList();
        }

        private async Task<TeacherOfMonthInfo?> GetPreviousMonthTopTeacher()
        {
            var lastMonth = DateTime.Now.AddMonths(-1);
            
            var topTeacher = await _context.TeacherPerformances
                .Include(tp => tp.Teacher)
                .ThenInclude(t => t.Campus)
                .Where(tp => tp.Month == lastMonth.Month &&
                            tp.Year == lastMonth.Year)
                .OrderByDescending(tp => tp.TotalScore)
                .FirstOrDefaultAsync();

            if (topTeacher == null || topTeacher.TotalScore < 16)
                return null;

            return new TeacherOfMonthInfo
            {
                Month = topTeacher.Month,
                Year = topTeacher.Year,
                Score = topTeacher.TotalScore,
                Achievements = "",
                TeacherName = topTeacher.Teacher.FullName,
                CampusName = topTeacher.Teacher.Campus?.Name ?? ""
            };
        }

        [HttpPost]
        public async Task<IActionResult> AddToDo([FromBody] ToDoRequest request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                
                var todo = new ToDo
                {
                    Title = request.Title,
                    Description = request.Description,
                    DueDate = request.DueDate,
                    Priority = request.Priority,
                    UserId = currentUser.Id,
                    CampusId = currentUser.CampusId,
                    IsCompleted = false,
                    CreatedAt = DateTime.Now,
                    CreatedBy = currentUser.UserName,
                    IsActive = true
                };

                _context.ToDos.Add(todo);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "To-Do added successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleToDo(int id)
        {
            try
            {
                var todo = await _context.ToDos.FindAsync(id);
                if (todo == null)
                    return Json(new { success = false, message = "To-Do not found" });

                todo.IsCompleted = !todo.IsCompleted;
                todo.UpdatedAt = DateTime.Now;
                
                await _context.SaveChangesAsync();
                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteToDo(int id)
        {
            try
            {
                var todo = await _context.ToDos.FindAsync(id);
                if (todo == null)
                    return Json(new { success = false, message = "To-Do not found" });

                todo.IsActive = false;
                await _context.SaveChangesAsync();
                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetToDoListData(int teacherId)
        {
            try
            {
                var todos = await GetToDoList(teacherId);
                return Json(new { success = true, todos });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendanceData(int teacherId, string period)
        {
            try
            {
                var today = DateTime.Today;
                DateTime startDate;
                DateTime endDate = today;
                List<string> labels;

                if (period == "thisYear")
                {
                    startDate = new DateTime(today.Year, 1, 1);
                    labels = new List<string> { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
                }
                else // lastYear
                {
                    startDate = new DateTime(today.Year - 1, 1, 1);
                    endDate = new DateTime(today.Year - 1, 12, 31);
                    labels = new List<string> { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
                }

                var attendanceRecords = await _context.EmployeeAttendance
                    .Where(ea => ea.EmployeeId == teacherId &&
                                ea.Date >= startDate &&
                                ea.Date <= endDate)
                    .ToListAsync();

                var presentData = new List<int>();
                var absentData = new List<int>();
                var leaveData = new List<int>();
                var lateData = new List<int>();

                for (int month = 1; month <= 12; month++)
                {
                    var monthRecords = attendanceRecords.Where(r => r.Date.Month == month).ToList();
                    presentData.Add(monthRecords.Count(r => r.Status == "P"));
                    absentData.Add(monthRecords.Count(r => r.Status == "A"));
                    leaveData.Add(monthRecords.Count(r => r.Status == "L"));
                    lateData.Add(monthRecords.Count(r => r.Status == "T" || r.Status == "S"));
                }

                var totals = new
                {
                    total = attendanceRecords.Count,
                    present = attendanceRecords.Count(r => r.Status == "P"),
                    absent = attendanceRecords.Count(r => r.Status == "A")
                };

                return Json(new
                {
                    success = true,
                    labels,
                    presentData,
                    absentData,
                    leaveData,
                    lateData,
                    totals
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class ToDoRequest
        {
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public DateTime DueDate { get; set; }
            public string Priority { get; set; } = "Medium";
        }

        // View Models
        public class TeacherDashboardViewModel
        {
            public Employee Teacher { get; set; } = null!;
            public string TeacherName { get; set; } = string.Empty;
            public int TeacherId { get; set; }
            public string? ProfilePicture { get; set; }
            public string CampusName { get; set; } = string.Empty;
            
            public decimal AttendancePercentage { get; set; }
            public List<EmployeeAttendance> RecentAttendance { get; set; } = new();
            public LeaveBalanceInfo? LeaveBalance { get; set; }
            
            public TeacherPayrollSummary PayrollDetails { get; set; } = new();
            
            public TeacherExamAnalysis ExamAnalysis { get; set; } = new();
            
            public List<TimetableEntry> TodayTimetable { get; set; } = new();
            public List<TimetableEntry> TomorrowTimetable { get; set; } = new();
            
            public List<CalendarEvent> CalendarEvents { get; set; } = new();
            public List<DiaryInfo> TodayDiaries { get; set; } = new();
            public List<AssignedDutyInfo> AssignedDuties { get; set; } = new();
            public List<ComplaintInfo> TeacherComplaints { get; set; } = new();
            public List<ToDoItem> ToDoList { get; set; } = new();
            
            public TeacherOfMonthInfo? TeacherOfMonth { get; set; }
            public TeacherOfMonthInfo? PreviousMonthTopTeacher { get; set; }
            public List<TeacherOfMonthInfo> AllTeacherAwards { get; set; } = new();
            
            public TeacherPerformance? CurrentPerformance { get; set; }
            
            public int TotalStudents { get; set; }
            public int StudentsToday { get; set; }
        }

        public class LeaveBalanceInfo
        {
            public int CasualTotal { get; set; }
            public int CasualUsed { get; set; }
            public int CasualRemaining { get; set; }
            
            public int SickTotal { get; set; }
            public int SickUsed { get; set; }
            public int SickRemaining { get; set; }
            
            public int AnnualTotal { get; set; }
            public int AnnualUsed { get; set; }
            public int AnnualRemaining { get; set; }
        }

        public class TeacherPayrollSummary
        {
            public decimal LastMonthGross { get; set; }
            public decimal LastMonthNet { get; set; }
            public decimal LastMonthDeductions { get; set; }
            public decimal LastMonthBasic { get; set; }
            public decimal LastMonthAllowances { get; set; }
            public decimal LastMonthBonus { get; set; }
            public decimal LastMonthPreviousBalance { get; set; }
            public decimal LastMonthAttendanceDeduction { get; set; }
            
            public decimal MonthBeforeLastGross { get; set; }
            public decimal MonthBeforeLastNet { get; set; }
            public decimal MonthBeforeLastDeductions { get; set; }
            public decimal MonthBeforeLastBasic { get; set; }
            public decimal MonthBeforeLastAllowances { get; set; }
            public decimal MonthBeforeLastBonus { get; set; }
            public decimal MonthBeforeLastPreviousBalance { get; set; }
            public decimal MonthBeforeLastAttendanceDeduction { get; set; }
            
            public PayrollMaster? LastMonthRecord { get; set; }
            public PayrollMaster? MonthBeforeLastRecord { get; set; }
            
            public int LastMonthMonth { get; set; }
            public int LastMonthYear { get; set; }
            public int MonthBeforeLastMonth { get; set; }
            public int MonthBeforeLastYear { get; set; }
        }

        public class TeacherExamAnalysis
        {
            public List<ClassWiseResult> ClassWiseResults { get; set; } = new();
            public List<TestWiseResult> TestWiseResults { get; set; } = new();
        }

        public class ClassWiseResult
        {
            public string ClassName { get; set; } = string.Empty;
            public string SubjectName { get; set; } = string.Empty;
            public int SubjectId { get; set; }
            public int ClassId { get; set; }
            public int SectionId { get; set; }
            public double AveragePercentage { get; set; }
            public int TotalStudents { get; set; }
            public int TotalTests { get; set; }
        }

        public class TestWiseResult
        {
            public int ExamId { get; set; }
            public string ExamName { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
            public int CategoryId { get; set; }
            public string SubjectName { get; set; } = string.Empty;
            public int SubjectId { get; set; }
            public string ClassName { get; set; } = string.Empty;
            public double AveragePercentage { get; set; }
            public int TotalStudents { get; set; }
            public DateTime ExamDate { get; set; }
        }

        public class TimetableEntry
        {
            public int Id { get; set; }
            public string ClassName { get; set; } = string.Empty;
            public string SectionName { get; set; } = string.Empty;
            public string SubjectName { get; set; } = string.Empty;
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
            public string DayOfWeek { get; set; } = string.Empty;
            public string? SubstitutionInfo { get; set; }
        }

        public class CalendarEvent
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public DateTime StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public bool IsHoliday { get; set; }
            public bool IsMultiDay { get; set; }
            public string EventType { get; set; } = "holiday";
        }

        public class DiaryInfo
        {
            public int Id { get; set; }
            public string ClassName { get; set; } = string.Empty;
            public string SectionName { get; set; } = string.Empty;
            public string SubjectName { get; set; } = string.Empty;
            public string TeacherName { get; set; } = string.Empty;
            public string Homework { get; set; } = string.Empty;
            public DateTime Date { get; set; }
            public int PendingCount { get; set; }
        }

        public class AssignedDutyInfo
        {
            public int Id { get; set; }
            public string DutyTitle { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public DateTime StartDate { get; set; }
            public DateTime DueDate { get; set; }
            public string Priority { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
        }

        public class ComplaintInfo
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string StudentName { get; set; } = string.Empty;
            public string ClassName { get; set; } = string.Empty;
            public string SectionName { get; set; } = string.Empty;
            public string Priority { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public DateTime ComplaintDate { get; set; }
            public string ComplaintType { get; set; } = string.Empty;
        }

        public class ToDoItem
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public bool IsCompleted { get; set; }
            public DateTime DueDate { get; set; }
            public string Priority { get; set; } = string.Empty;
        }

        public class TeacherOfMonthInfo
        {
            public int Month { get; set; }
            public int Year { get; set; }
            public decimal Score { get; set; }
            public string Achievements { get; set; } = string.Empty;
            public string? TeacherName { get; set; }
            public string? CampusName { get; set; }
        }
    }
}
