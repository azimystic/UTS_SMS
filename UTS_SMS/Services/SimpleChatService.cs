using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using UTS_SMS.Models;

namespace UTS_SMS.Services
{
    /// <summary>
    /// AI-powered chat service using Groq for intent detection and database queries for data.
    /// Handles all edge cases including typos, date formats, class name variations.
    /// </summary>
    public class SimpleChatService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SimpleChatService> _logger;

        // Class name mappings for variations
        private static readonly Dictionary<string, string[]> ClassNameMappings = new(StringComparer.OrdinalIgnoreCase)
        {
            { "1", new[] { "one", "first", "1st", "i", "class 1", "grade 1" } },
            { "2", new[] { "two", "second", "2nd", "ii", "class 2", "grade 2" } },
            { "3", new[] { "three", "third", "3rd", "iii", "class 3", "grade 3" } },
            { "4", new[] { "four", "fourth", "4th", "iv", "class 4", "grade 4" } },
            { "5", new[] { "five", "fifth", "5th", "v", "class 5", "grade 5" } },
            { "6", new[] { "six", "sixth", "6th", "vi", "class 6", "grade 6" } },
            { "7", new[] { "seven", "seventh", "7th", "vii", "class 7", "grade 7" } },
            { "8", new[] { "eight", "eighth", "8th", "viii", "class 8", "grade 8" } },
            { "9", new[] { "nine", "ninth", "9th", "ix", "class 9", "grade 9" } },
            { "10", new[] { "ten", "tenth", "10th", "x", "class 10", "grade 10" } },
            { "11", new[] { "eleven", "eleventh", "11th", "xi", "class 11", "grade 11", "first year", "1st year" } },
            { "12", new[] { "twelve", "twelfth", "12th", "xii", "class 12", "grade 12", "second year", "2nd year" } },
        };

        public SimpleChatService(
            ApplicationDbContext context,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<SimpleChatService> logger)
        {
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<ChatResponse> ProcessMessageAsync(
            string message,
            List<ChatMessage> conversationHistory,
            ChatUserContext userContext)
        {
            try
            {
                // Use AI to understand intent and extract parameters
                var intent = await GetIntentFromAIAsync(message, conversationHistory, userContext);
                
                if (intent == null)
                {
                    // If AI fails, use general conversation
                    return await GetGeneralResponseAsync(message, conversationHistory, userContext);
                }

                // Execute based on detected intent
                var response = await ExecuteIntentAsync(intent, userContext);
                return new ChatResponse { Success = true, Message = response };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat message");
                return new ChatResponse
                {
                    Success = false,
                    Message = "Sorry, I encountered an error processing your request. Please try again."
                };
            }
        }

        private async Task<ChatIntent?> GetIntentFromAIAsync(
            string message,
            List<ChatMessage> conversationHistory,
            ChatUserContext userContext)
        {
            try
            {
                var apiKey = _configuration["AiChat:GroqApiKey"];
                var apiUrl = _configuration["AiChat:GroqApiUrl"] ?? "https://api.groq.com/openai/v1";
                var models = _configuration.GetSection("AiChat:GroqModels").Get<string[]>() ?? new[] { "llama-3.1-8b-instant" };

                if (string.IsNullOrEmpty(apiKey))
                    return null;

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                // Get available data for context
                var classes = await _context.Classes.Where(c => c.IsActive).Select(c => c.Name).ToListAsync();
                var sections = await _context.ClassSections.Where(s => s.IsActive).Select(s => s.Name).Distinct().ToListAsync();
                var examCategories = await _context.Set<ExamCategory>().Where(e => e.IsActive).Select(e => e.Name).ToListAsync();
                var exams = await _context.Set<Exam>().Where(e => e.IsActive).Select(e => e.Name).ToListAsync();

                var systemPrompt = $@"You are an intent parser for a School Management System. Analyze the user message and extract structured intent.
Today's date is {DateTime.Now:yyyy-MM-dd}. Current user: {userContext.FullName} ({userContext.Role}).

Available Classes: {string.Join(", ", classes)}
Available Sections: {string.Join(", ", sections)}
Available Exam Categories: {string.Join(", ", examCategories)}
Available Exams: {string.Join(", ", exams)}

IMPORTANT: Handle class name variations like '9', 'Nine', 'ninth', '9th', 'IX' all mean class 9.
Handle date variations like 'today', 'yesterday', 'last monday', '15 jan', '2024-01-15', '15/01/2024'.

Return a JSON object with these fields:
{{
    ""intent"": ""student_attendance|class_attendance|section_attendance|employee_attendance|student_marks|class_marks|student_count|employee_count|student_info|employee_info|class_list|exam_list|general"",
    ""studentName"": ""extracted student name or null"",
    ""employeeName"": ""extracted employee name or null"",
    ""className"": ""normalized class number like 9, 10 or null"",
    ""sectionName"": ""section letter like A, B or null"",
    ""examCategory"": ""exam category name or null"",
    ""examName"": ""specific exam name or null"",
    ""subjectName"": ""subject name or null"",
    ""date"": ""YYYY-MM-DD format or null"",
    ""dateRange"": ""last_week|last_month|this_month|custom or null"",
    ""isMyData"": true/false (if user asks about their own data)
}}

Only return the JSON object, no other text. If the query is general conversation or you can't determine intent, set intent to ""general"".";

                var messages = new List<object>
                {
                    new { role = "system", content = systemPrompt }
                };

                // Add recent conversation context
                foreach (var msg in conversationHistory.TakeLast(4))
                {
                    messages.Add(new { role = msg.Role, content = msg.Content });
                }
                messages.Add(new { role = "user", content = message });

                foreach (var model in models)
                {
                    try
                    {
                        var requestBody = new
                        {
                            model = model,
                            messages = messages,
                            max_tokens = 500,
                            temperature = 0.1
                        };

                        var response = await client.PostAsync(
                            $"{apiUrl}/chat/completions",
                            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));

                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            var jsonDoc = JsonDocument.Parse(responseContent);
                            var aiResponse = jsonDoc.RootElement
                                .GetProperty("choices")[0]
                                .GetProperty("message")
                                .GetProperty("content")
                                .GetString();

                            if (!string.IsNullOrEmpty(aiResponse))
                            {
                                // Clean the response - remove markdown code blocks if present
                                aiResponse = aiResponse.Trim();
                                if (aiResponse.StartsWith("```"))
                                {
                                    var lines = aiResponse.Split('\n');
                                    aiResponse = string.Join("\n", lines.Skip(1).TakeWhile(l => !l.StartsWith("```")));
                                }

                                var intent = JsonSerializer.Deserialize<ChatIntent>(aiResponse, new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                });
                                return intent;
                            }
                        }

                        if ((int)response.StatusCode == 429)
                        {
                            _logger.LogWarning("Rate limit hit for model {Model} in intent detection", model);
                            await Task.Delay(500);
                            continue;
                        }
                    }
                    catch (JsonException jex)
                    {
                        _logger.LogWarning(jex, "Failed to parse AI response as JSON");
                        continue;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error with model {Model} in intent detection", model);
                        continue;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting intent from AI");
                return null;
            }
        }

        private async Task<string> ExecuteIntentAsync(ChatIntent intent, ChatUserContext ctx)
        {
            return intent.Intent?.ToLower() switch
            {
                "student_attendance" => await HandleStudentAttendanceAsync(intent, ctx),
                "class_attendance" => await HandleClassAttendanceAsync(intent, ctx),
                "section_attendance" => await HandleSectionAttendanceAsync(intent, ctx),
                "employee_attendance" => await HandleEmployeeAttendanceAsync(intent, ctx),
                "student_marks" => await HandleStudentMarksAsync(intent, ctx),
                "class_marks" => await HandleClassMarksAsync(intent, ctx),
                "student_count" => await GetStudentCountAsync(ctx),
                "employee_count" => await GetEmployeeCountAsync(ctx),
                "student_info" => await HandleStudentInfoAsync(intent, ctx),
                "employee_info" => await HandleEmployeeInfoAsync(intent, ctx),
                "class_list" => await GetClassListAsync(ctx),
                "exam_list" => await GetExamListAsync(ctx),
                _ => await GetGeneralResponseTextAsync(intent, ctx)
            };
        }

        // ???????????????????????????????????????????????????????????????????????
        // ATTENDANCE HANDLERS
        // ???????????????????????????????????????????????????????????????????????

        private async Task<string> HandleStudentAttendanceAsync(ChatIntent intent, ChatUserContext ctx)
        {
            // If asking about own attendance
            if (intent.IsMyData && ctx.StudentId.HasValue)
            {
                return await GetStudentAttendanceByIdAsync(ctx.StudentId.Value, intent, ctx);
            }

            // If student name provided
            if (!string.IsNullOrEmpty(intent.StudentName))
            {
                return await GetStudentAttendanceByNameAsync(intent.StudentName, intent, ctx);
            }

            return "Please specify a student name. For example: 'Attendance of Ahmed Khan' or 'Show my attendance'";
        }

        private async Task<string> GetStudentAttendanceByNameAsync(string name, ChatIntent intent, ChatUserContext ctx)
        {
            var query = _context.Students.Where(s => !s.HasLeft);
            
            if (ctx.CampusId.HasValue)
                query = query.Where(s => s.CampusId == ctx.CampusId);

            // Search with fuzzy matching
            var students = await query
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .ToListAsync();

            var student = students.FirstOrDefault(s => 
                s.StudentName.Contains(name, StringComparison.OrdinalIgnoreCase) ||
                FuzzyMatch(s.StudentName, name));

            if (student == null)
                return $"No student found with name '{name}'. Please check the spelling and try again.";

            return await GetStudentAttendanceByIdAsync(student.Id, intent, ctx, student);
        }

        private async Task<string> GetStudentAttendanceByIdAsync(int studentId, ChatIntent intent, ChatUserContext ctx, Student? student = null)
        {
            student ??= await _context.Students
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                return "Student not found.";

            var query = _context.Attendance.Where(a => a.StudentId == studentId);

            // Apply date filter
            var (startDate, endDate) = GetDateRange(intent);
            if (startDate.HasValue)
                query = query.Where(a => a.Date >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(a => a.Date <= endDate.Value);

            var attendance = await query.OrderByDescending(a => a.Date).Take(30).ToListAsync();

            if (!attendance.Any())
                return $"No attendance records found for {student.StudentName} in the specified period.";

            var total = attendance.Count;
            var present = attendance.Count(a => a.Status == "P");
            var absent = attendance.Count(a => a.Status == "A");
            var leave = attendance.Count(a => a.Status == "L");
            var percentage = total > 0 ? (double)present / total * 100 : 0;

            var recent = attendance.Take(7)
                .Select(a => $"  {a.Date:dd MMM yyyy}: {GetStatusText(a.Status)}")
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"Attendance Report - {student.StudentName}");
            sb.AppendLine($"Class: {student.ClassObj?.Name} - {student.SectionObj?.Name}");
            sb.AppendLine();
            sb.AppendLine("Summary:");
            sb.AppendLine($"  Total Days: {total}");
            sb.AppendLine($"  Present: {present} ({percentage:F1}%)");
            sb.AppendLine($"  Absent: {absent}");
            sb.AppendLine($"  Leave: {leave}");
            sb.AppendLine();
            sb.AppendLine("Recent Records:");
            sb.AppendLine(string.Join("\n", recent));

            return sb.ToString();
        }

        private async Task<string> HandleClassAttendanceAsync(ChatIntent intent, ChatUserContext ctx)
        {
            var classId = await ResolveClassIdAsync(intent.ClassName);
            if (!classId.HasValue)
                return "Please specify a valid class. For example: 'Class 9 attendance' or 'Attendance of ninth class'.";

            return await GetClassAttendanceAsync(classId.Value, null, intent, ctx);
        }

        private async Task<string> HandleSectionAttendanceAsync(ChatIntent intent, ChatUserContext ctx)
        {
            var classId = await ResolveClassIdAsync(intent.ClassName);
            if (!classId.HasValue)
                return "Please specify a valid class. For example: 'Class 9 Section A attendance'.";

            int? sectionId = null;
            if (!string.IsNullOrEmpty(intent.SectionName))
            {
                var section = await _context.ClassSections
                    .FirstOrDefaultAsync(s => s.ClassId == classId && s.IsActive &&
                        s.Name.Equals(intent.SectionName, StringComparison.OrdinalIgnoreCase));
                sectionId = section?.Id;
            }

            return await GetClassAttendanceAsync(classId.Value, sectionId, intent, ctx);
        }

        private async Task<string> GetClassAttendanceAsync(int classId, int? sectionId, ChatIntent intent, ChatUserContext ctx)
        {
            var (startDate, endDate) = GetDateRange(intent);
            var targetDate = startDate ?? DateTime.Today;

            var query = _context.Attendance
                .Include(a => a.Student)
                .Include(a => a.ClassObj)
                .Include(a => a.SectionObj)
                .Where(a => a.ClassId == classId && a.Date.Date == targetDate.Date);

            if (sectionId.HasValue)
                query = query.Where(a => a.SectionId == sectionId);

            if (ctx.CampusId.HasValue)
                query = query.Where(a => a.CampusId == ctx.CampusId);

            var attendance = await query.ToListAsync();

            // If no data for target date, find latest available
            if (!attendance.Any())
            {
                var latestQuery = _context.Attendance
                    .Include(a => a.Student)
                    .Include(a => a.ClassObj)
                    .Include(a => a.SectionObj)
                    .Where(a => a.ClassId == classId);

                if (sectionId.HasValue)
                    latestQuery = latestQuery.Where(a => a.SectionId == sectionId);
                if (ctx.CampusId.HasValue)
                    latestQuery = latestQuery.Where(a => a.CampusId == ctx.CampusId);

                var latestDate = await latestQuery.OrderByDescending(a => a.Date).Select(a => a.Date).FirstOrDefaultAsync();
                if (latestDate == default)
                    return "No attendance records found for this class.";

                targetDate = latestDate;
                attendance = await latestQuery.Where(a => a.Date.Date == latestDate.Date).ToListAsync();
            }

            var className = attendance.FirstOrDefault()?.ClassObj?.Name ?? "Unknown";
            
            // Group by section if no specific section requested
            if (!sectionId.HasValue)
            {
                var bySection = attendance.GroupBy(a => a.SectionObj?.Name ?? "Unknown")
                    .OrderBy(g => g.Key)
                    .ToList();

                var sb = new StringBuilder();
                sb.AppendLine($"Class {className} Attendance - {targetDate:dd MMM yyyy}");
                sb.AppendLine();

                foreach (var section in bySection)
                {
                    var sTotal = section.Count();
                    var sPresent = section.Count(a => a.Status == "P");
                    var sAbsent = section.Count(a => a.Status == "A");
                    var sPct = sTotal > 0 ? (double)sPresent / sTotal * 100 : 0;

                    sb.AppendLine($"Section {section.Key}:");
                    sb.AppendLine($"  Total: {sTotal}, Present: {sPresent} ({sPct:F1}%), Absent: {sAbsent}");
                    
                    var absentStudents = section.Where(a => a.Status == "A").Select(a => a.Student?.StudentName).Take(5).ToList();
                    if (absentStudents.Any())
                    {
                        sb.AppendLine($"  Absent: {string.Join(", ", absentStudents)}");
                    }
                    sb.AppendLine();
                }

                var grandTotal = attendance.Count;
                var grandPresent = attendance.Count(a => a.Status == "P");
                sb.AppendLine($"Overall: {grandPresent}/{grandTotal} Present ({(grandTotal > 0 ? (double)grandPresent / grandTotal * 100 : 0):F1}%)");

                return sb.ToString();
            }
            else
            {
                var sectionName = attendance.FirstOrDefault()?.SectionObj?.Name ?? "";
                var total = attendance.Count;
                var present = attendance.Count(a => a.Status == "P");
                var absent = attendance.Count(a => a.Status == "A");
                var leave = attendance.Count(a => a.Status == "L");
                var percentage = total > 0 ? (double)present / total * 100 : 0;

                var absentStudents = attendance
                    .Where(a => a.Status == "A")
                    .Select(a => $"  - {a.Student?.StudentName}")
                    .ToList();

                var sb = new StringBuilder();
                sb.AppendLine($"Class {className} Section {sectionName} Attendance");
                sb.AppendLine($"Date: {targetDate:dd MMM yyyy}");
                sb.AppendLine();
                sb.AppendLine($"Total Students: {total}");
                sb.AppendLine($"Present: {present} ({percentage:F1}%)");
                sb.AppendLine($"Absent: {absent}");
                sb.AppendLine($"Leave: {leave}");

                if (absentStudents.Any())
                {
                    sb.AppendLine();
                    sb.AppendLine("Absent Students:");
                    sb.AppendLine(string.Join("\n", absentStudents));
                }
                else if (absent == 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("All students present!");
                }

                return sb.ToString();
            }
        }

        private async Task<string> HandleEmployeeAttendanceAsync(ChatIntent intent, ChatUserContext ctx)
        {
            if (!string.IsNullOrEmpty(intent.EmployeeName))
            {
                return await GetEmployeeAttendanceByNameAsync(intent.EmployeeName, intent, ctx);
            }

            // If asking about own attendance (employee)
            if (intent.IsMyData && ctx.EmployeeId.HasValue)
            {
                return await GetEmployeeAttendanceByIdAsync(ctx.EmployeeId.Value, intent, ctx);
            }

            // Overall employee attendance
            return await GetOverallEmployeeAttendanceAsync(intent, ctx);
        }

        private async Task<string> GetEmployeeAttendanceByNameAsync(string name, ChatIntent intent, ChatUserContext ctx)
        {
            var employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
            
            var employee = employees.FirstOrDefault(e => 
                e.FullName.Contains(name, StringComparison.OrdinalIgnoreCase) ||
                FuzzyMatch(e.FullName, name));

            if (employee == null)
                return $"No employee found with name '{name}'. Please check the spelling and try again.";

            return await GetEmployeeAttendanceByIdAsync(employee.Id, intent, ctx, employee);
        }

        private async Task<string> GetEmployeeAttendanceByIdAsync(int employeeId, ChatIntent intent, ChatUserContext ctx, Employee? employee = null)
        {
            employee ??= await _context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null)
                return "Employee not found.";

            var query = _context.EmployeeAttendance.Where(a => a.EmployeeId == employeeId);

            var (startDate, endDate) = GetDateRange(intent);
            if (startDate.HasValue)
                query = query.Where(a => a.Date >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(a => a.Date <= endDate.Value);

            var attendance = await query.OrderByDescending(a => a.Date).Take(30).ToListAsync();

            if (!attendance.Any())
                return $"No attendance records found for {employee.FullName}.";

            var total = attendance.Count;
            var present = attendance.Count(a => a.Status == "P");
            var absent = attendance.Count(a => a.Status == "A");
            var leave = attendance.Count(a => a.Status == "L" || a.Status == "S");
            var percentage = total > 0 ? (double)present / total * 100 : 0;

            var recent = attendance.Take(7)
                .Select(a => $"  {a.Date:dd MMM yyyy}: {GetStatusText(a.Status)}")
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"Attendance Report - {employee.FullName}");
            sb.AppendLine($"Role: {employee.Role}");
            sb.AppendLine();
            sb.AppendLine("Summary (Last 30 Days):");
            sb.AppendLine($"  Total Days: {total}");
            sb.AppendLine($"  Present: {present} ({percentage:F1}%)");
            sb.AppendLine($"  Absent: {absent}");
            sb.AppendLine($"  Leave: {leave}");
            sb.AppendLine();
            sb.AppendLine("Recent Records:");
            sb.AppendLine(string.Join("\n", recent));

            return sb.ToString();
        }

        private async Task<string> GetOverallEmployeeAttendanceAsync(ChatIntent intent, ChatUserContext ctx)
        {
            var (startDate, endDate) = GetDateRange(intent);
            var targetDate = startDate ?? DateTime.Today;

            var query = _context.EmployeeAttendance.Include(a => a.Employee).Where(a => a.Date.Date == targetDate.Date);

            var attendance = await query.ToListAsync();

            if (!attendance.Any())
            {
                var latestDate = await _context.EmployeeAttendance
                    .OrderByDescending(a => a.Date)
                    .Select(a => a.Date)
                    .FirstOrDefaultAsync();

                if (latestDate != default)
                {
                    targetDate = latestDate;
                    attendance = await _context.EmployeeAttendance
                        .Include(a => a.Employee)
                        .Where(a => a.Date.Date == targetDate.Date)
                        .ToListAsync();
                }
            }

            if (!attendance.Any())
                return "No employee attendance records found.";

            var total = attendance.Count;
            var present = attendance.Count(a => a.Status == "P");
            var absent = attendance.Count(a => a.Status == "A");
            var leave = attendance.Count(a => a.Status == "L" || a.Status == "S");
            var pct = total > 0 ? (double)present / total * 100 : 0;

            var byRole = attendance
                .GroupBy(a => a.Employee?.Role ?? "Unknown")
                .Select(g => $"  {g.Key}: {g.Count(a => a.Status == "P")}/{g.Count()} Present")
                .ToList();

            var absentList = attendance
                .Where(a => a.Status == "A")
                .Select(a => $"  - {a.Employee?.FullName} ({a.Employee?.Role})")
                .Take(10)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"Employee Attendance - {targetDate:dd MMM yyyy}");
            sb.AppendLine();
            sb.AppendLine("Summary:");
            sb.AppendLine($"  Total: {total}");
            sb.AppendLine($"  Present: {present} ({pct:F1}%)");
            sb.AppendLine($"  Absent: {absent}");
            sb.AppendLine($"  Leave: {leave}");
            sb.AppendLine();
            sb.AppendLine("By Role:");
            sb.AppendLine(string.Join("\n", byRole));

            if (absentList.Any())
            {
                sb.AppendLine();
                sb.AppendLine("Absent Employees:");
                sb.AppendLine(string.Join("\n", absentList));
            }

            return sb.ToString();
        }

        // ???????????????????????????????????????????????????????????????????????
        // EXAM MARKS HANDLERS
        // ???????????????????????????????????????????????????????????????????????

        private async Task<string> HandleStudentMarksAsync(ChatIntent intent, ChatUserContext ctx)
        {
            if (intent.IsMyData && ctx.StudentId.HasValue)
            {
                return await GetStudentMarksAsync(ctx.StudentId.Value, intent, ctx);
            }

            if (!string.IsNullOrEmpty(intent.StudentName))
            {
                var students = await _context.Students.Where(s => !s.HasLeft).ToListAsync();
                var student = students.FirstOrDefault(s =>
                    s.StudentName.Contains(intent.StudentName, StringComparison.OrdinalIgnoreCase) ||
                    FuzzyMatch(s.StudentName, intent.StudentName));

                if (student == null)
                    return $"No student found with name '{intent.StudentName}'. Please check the spelling.";

                return await GetStudentMarksAsync(student.Id, intent, ctx, student);
            }

            return "Please specify a student name. For example: 'Marks of Ahmed Khan' or 'Show my marks'";
        }

        private async Task<string> GetStudentMarksAsync(int studentId, ChatIntent intent, ChatUserContext ctx, Student? student = null)
        {
            student ??= await _context.Students
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                return "Student not found.";

            var query = _context.ExamMarks
                .Include(m => m.Subject)
                .Include(m => m.Exam)
                .ThenInclude(e => e.ExamCategory)
                .Where(m => m.StudentId == studentId && m.IsActive);

            // Filter by exam category if specified
            if (!string.IsNullOrEmpty(intent.ExamCategory))
            {
                var categories = await _context.Set<ExamCategory>().Where(c => c.IsActive).ToListAsync();
                var category = categories.FirstOrDefault(c =>
                    c.Name.Contains(intent.ExamCategory, StringComparison.OrdinalIgnoreCase) ||
                    FuzzyMatch(c.Name, intent.ExamCategory));

                if (category != null)
                {
                    query = query.Where(m => m.Exam.ExamCategoryId == category.Id);
                }
            }

            // Filter by specific exam if specified
            if (!string.IsNullOrEmpty(intent.ExamName))
            {
                var exams = await _context.Set<Exam>().Where(e => e.IsActive).ToListAsync();
                var exam = exams.FirstOrDefault(e =>
                    e.Name.Contains(intent.ExamName, StringComparison.OrdinalIgnoreCase) ||
                    FuzzyMatch(e.Name, intent.ExamName));

                if (exam != null)
                {
                    query = query.Where(m => m.ExamId == exam.Id);
                }
            }

            // Filter by subject if specified
            if (!string.IsNullOrEmpty(intent.SubjectName))
            {
                var subjects = await _context.Subjects.Where(s => s.IsActive).ToListAsync();
                var subject = subjects.FirstOrDefault(s =>
                    s.Name.Contains(intent.SubjectName, StringComparison.OrdinalIgnoreCase) ||
                    FuzzyMatch(s.Name, intent.SubjectName));

                if (subject != null)
                {
                    query = query.Where(m => m.SubjectId == subject.Id);
                }
            }

            var marks = await query.OrderByDescending(m => m.ExamDate).Take(30).ToListAsync();

            if (!marks.Any())
            {
                var filterMsg = "";
                if (!string.IsNullOrEmpty(intent.ExamCategory)) filterMsg += $" for {intent.ExamCategory}";
                if (!string.IsNullOrEmpty(intent.ExamName)) filterMsg += $" in {intent.ExamName}";
                if (!string.IsNullOrEmpty(intent.SubjectName)) filterMsg += $" in {intent.SubjectName}";
                return $"No exam results found for {student.StudentName}{filterMsg}.";
            }

            var avgPercentage = marks.Average(m => m.Percentage);
            var totalObtained = marks.Sum(m => m.ObtainedMarks);
            var totalPossible = marks.Sum(m => m.TotalMarks);

            var sb = new StringBuilder();
            sb.AppendLine($"Exam Results - {student.StudentName}");
            sb.AppendLine($"Class: {student.ClassObj?.Name} - {student.SectionObj?.Name}");
            
            if (!string.IsNullOrEmpty(intent.ExamCategory) || !string.IsNullOrEmpty(intent.ExamName))
            {
                var examInfo = marks.FirstOrDefault()?.Exam;
                if (examInfo != null)
                {
                    sb.AppendLine($"Exam: {examInfo.ExamCategory?.Name} - {examInfo.Name}");
                }
            }
            
            sb.AppendLine();
            sb.AppendLine("Overall Performance:");
            sb.AppendLine($"  Average: {avgPercentage:F1}%");
            sb.AppendLine($"  Total: {totalObtained:F0}/{totalPossible:F0}");
            sb.AppendLine();

            // Group by exam if showing multiple exams
            var byExam = marks.GroupBy(m => new { CategoryName = m.Exam?.ExamCategory?.Name, ExamName = m.Exam?.Name })
                .OrderByDescending(g => g.First().ExamDate)
                .ToList();

            if (byExam.Count > 1)
            {
                sb.AppendLine("By Exam:");
                foreach (var examGroup in byExam.Take(5))
                {
                    var examAvg = examGroup.Average(m => m.Percentage);
                    sb.AppendLine($"  {examGroup.Key.ExamName} ({examGroup.Key.CategoryName}): {examAvg:F1}%");
                }
                sb.AppendLine();
            }

            // By Subject
            var bySubject = marks.GroupBy(m => m.Subject?.Name ?? "Unknown")
                .OrderByDescending(g => g.Average(m => m.Percentage))
                .ToList();

            sb.AppendLine("By Subject:");
            foreach (var subj in bySubject)
            {
                sb.AppendLine($"  {subj.Key}: {subj.Average(m => m.Percentage):F1}%");
            }
            sb.AppendLine();

            // Recent results
            var recentResults = marks.Take(5)
                .Select(m => $"  {m.Subject?.Name} ({m.Exam?.Name}): {m.ObtainedMarks:F0}/{m.TotalMarks:F0} ({m.Percentage:F1}%) - Grade {m.Grade}")
                .ToList();

            sb.AppendLine("Recent Results:");
            sb.AppendLine(string.Join("\n", recentResults));

            return sb.ToString();
        }

        private async Task<string> HandleClassMarksAsync(ChatIntent intent, ChatUserContext ctx)
        {
            var classId = await ResolveClassIdAsync(intent.ClassName);
            if (!classId.HasValue)
                return "Please specify a valid class. For example: 'Class 9 marks' or 'Performance of tenth class'.";

            var query = _context.ExamMarks
                .Include(m => m.Student)
                .Include(m => m.Subject)
                .Include(m => m.Class)
                .Include(m => m.Section)
                .Include(m => m.Exam)
                .ThenInclude(e => e.ExamCategory)
                .Where(m => m.ClassId == classId && m.IsActive);

            // Filter by section
            if (!string.IsNullOrEmpty(intent.SectionName))
            {
                var section = await _context.ClassSections
                    .FirstOrDefaultAsync(s => s.ClassId == classId && s.IsActive &&
                        s.Name.Equals(intent.SectionName, StringComparison.OrdinalIgnoreCase));
                if (section != null)
                    query = query.Where(m => m.SectionId == section.Id);
            }

            // Filter by exam category
            if (!string.IsNullOrEmpty(intent.ExamCategory))
            {
                var categories = await _context.Set<ExamCategory>().Where(c => c.IsActive).ToListAsync();
                var category = categories.FirstOrDefault(c =>
                    c.Name.Contains(intent.ExamCategory, StringComparison.OrdinalIgnoreCase) ||
                    FuzzyMatch(c.Name, intent.ExamCategory));
                if (category != null)
                    query = query.Where(m => m.Exam.ExamCategoryId == category.Id);
            }

            // Filter by specific exam
            if (!string.IsNullOrEmpty(intent.ExamName))
            {
                var exams = await _context.Set<Exam>().Where(e => e.IsActive).ToListAsync();
                var exam = exams.FirstOrDefault(e =>
                    e.Name.Contains(intent.ExamName, StringComparison.OrdinalIgnoreCase) ||
                    FuzzyMatch(e.Name, intent.ExamName));
                if (exam != null)
                    query = query.Where(m => m.ExamId == exam.Id);
            }

            if (ctx.CampusId.HasValue)
                query = query.Where(m => m.CampusId == ctx.CampusId);

            var marks = await query.ToListAsync();

            if (!marks.Any())
                return "No exam results found for this class with the specified criteria.";

            var className = marks.First().Class?.Name ?? "Unknown";
            var sectionName = !string.IsNullOrEmpty(intent.SectionName) ? $" Section {intent.SectionName}" : "";
            var avgPct = marks.Average(m => m.Percentage);
            var passRate = marks.Count > 0 ? (double)marks.Count(m => m.Status != "Fail") / marks.Count * 100 : 0;

            var sb = new StringBuilder();
            sb.AppendLine($"Class {className}{sectionName} Performance");
            
            var examInfo = marks.FirstOrDefault()?.Exam;
            if (examInfo != null && (!string.IsNullOrEmpty(intent.ExamCategory) || !string.IsNullOrEmpty(intent.ExamName)))
            {
                sb.AppendLine($"Exam: {examInfo.ExamCategory?.Name} - {examInfo.Name}");
            }
            
            sb.AppendLine();
            sb.AppendLine("Summary:");
            sb.AppendLine($"  Class Average: {avgPct:F1}%");
            sb.AppendLine($"  Pass Rate: {passRate:F1}%");
            sb.AppendLine($"  Total Records: {marks.Count}");
            sb.AppendLine();

            // Top performers
            var topStudents = marks
                .GroupBy(m => new { m.StudentId, m.Student?.StudentName })
                .Select(g => new { g.Key.StudentName, Avg = g.Average(m => m.Percentage) })
                .OrderByDescending(x => x.Avg)
                .Take(5)
                .ToList();

            sb.AppendLine("Top 5 Students:");
            for (int i = 0; i < topStudents.Count; i++)
            {
                sb.AppendLine($"  {i + 1}. {topStudents[i].StudentName}: {topStudents[i].Avg:F1}%");
            }
            sb.AppendLine();

            // By Subject
            var bySubject = marks.GroupBy(m => m.Subject?.Name ?? "Unknown")
                .OrderByDescending(g => g.Average(m => m.Percentage))
                .ToList();

            sb.AppendLine("Subject-wise Average:");
            foreach (var subj in bySubject)
            {
                sb.AppendLine($"  {subj.Key}: {subj.Average(m => m.Percentage):F1}%");
            }

            return sb.ToString();
        }

        // ???????????????????????????????????????????????????????????????????????
        // COUNT AND INFO HANDLERS
        // ???????????????????????????????????????????????????????????????????????

        private async Task<string> GetStudentCountAsync(ChatUserContext ctx)
        {
            var query = _context.Students.Where(s => !s.HasLeft);
            if (ctx.CampusId.HasValue)
                query = query.Where(s => s.CampusId == ctx.CampusId);

            var students = await query.Include(s => s.ClassObj).ToListAsync();
            var total = students.Count;
            var boys = students.Count(s => s.Gender == "Male");
            var girls = students.Count(s => s.Gender == "Female");

            var byClass = students
                .GroupBy(s => s.ClassObj?.Name ?? "Unknown")
                .OrderBy(g => g.Key)
                .Select(g => $"  {g.Key}: {g.Count()} students")
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Student Statistics");
            sb.AppendLine();
            sb.AppendLine($"Total Students: {total}");
            sb.AppendLine($"  Boys: {boys}");
            sb.AppendLine($"  Girls: {girls}");
            sb.AppendLine();
            sb.AppendLine("By Class:");
            sb.AppendLine(string.Join("\n", byClass));

            return sb.ToString();
        }

        private async Task<string> GetEmployeeCountAsync(ChatUserContext ctx)
        {
            var query = _context.Employees.Where(e => e.IsActive);
            if (ctx.CampusId.HasValue)
                query = query.Where(e => e.CampusId == ctx.CampusId);

            var employees = await query.ToListAsync();
            var total = employees.Count;
            var male = employees.Count(e => e.Gender == "Male");
            var female = employees.Count(e => e.Gender == "Female");

            var byRole = employees
                .GroupBy(e => e.Role ?? "Unknown")
                .OrderByDescending(g => g.Count())
                .Select(g => $"  {g.Key}: {g.Count()}")
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Employee Statistics");
            sb.AppendLine();
            sb.AppendLine($"Total Employees: {total}");
            sb.AppendLine($"  Male: {male}");
            sb.AppendLine($"  Female: {female}");
            sb.AppendLine();
            sb.AppendLine("By Role:");
            sb.AppendLine(string.Join("\n", byRole));

            return sb.ToString();
        }

        private async Task<string> HandleStudentInfoAsync(ChatIntent intent, ChatUserContext ctx)
        {
            if (string.IsNullOrEmpty(intent.StudentName))
                return "Please specify a student name. For example: 'Student info Ahmed Khan'";

            var students = await _context.Students
                .Include(s => s.ClassObj)
                .Include(s => s.SectionObj)
                .Include(s => s.Campus)
                .Where(s => !s.HasLeft)
                .ToListAsync();

            var student = students.FirstOrDefault(s =>
                s.StudentName.Contains(intent.StudentName, StringComparison.OrdinalIgnoreCase) ||
                FuzzyMatch(s.StudentName, intent.StudentName));

            if (student == null)
                return $"No student found with name '{intent.StudentName}'. Please check the spelling.";

            var sb = new StringBuilder();
            sb.AppendLine($"Student Information");
            sb.AppendLine();
            sb.AppendLine($"Name: {student.StudentName}");
            sb.AppendLine($"Roll Number: {student.RollNumber ?? "Not assigned"}");
            sb.AppendLine($"Class: {student.ClassObj?.Name} - {student.SectionObj?.Name}");
            sb.AppendLine($"Gender: {student.Gender}");
            sb.AppendLine($"Date of Birth: {student.DateOfBirth:dd MMM yyyy}");
            sb.AppendLine($"Campus: {student.Campus?.Name}");
            sb.AppendLine($"Father's Name: {student.FatherName}");
            sb.AppendLine($"Father's Phone: {student.FatherPhone}");
            sb.AppendLine($"Address: {student.HomeAddress}");
            sb.AppendLine($"Registration Date: {student.RegistrationDate:dd MMM yyyy}");

            return sb.ToString();
        }

        private async Task<string> HandleEmployeeInfoAsync(ChatIntent intent, ChatUserContext ctx)
        {
            if (string.IsNullOrEmpty(intent.EmployeeName))
                return "Please specify an employee name. For example: 'Employee info John Smith'";

            var employees = await _context.Employees
                .Include(e => e.Campus)
                .Where(e => e.IsActive)
                .ToListAsync();

            var employee = employees.FirstOrDefault(e =>
                e.FullName.Contains(intent.EmployeeName, StringComparison.OrdinalIgnoreCase) ||
                FuzzyMatch(e.FullName, intent.EmployeeName));

            if (employee == null)
                return $"No employee found with name '{intent.EmployeeName}'. Please check the spelling.";

            var sb = new StringBuilder();
            sb.AppendLine("Employee Information");
            sb.AppendLine();
            sb.AppendLine($"Name: {employee.FullName}");
            sb.AppendLine($"Role: {employee.Role}");
            sb.AppendLine($"Gender: {employee.Gender}");
            sb.AppendLine($"CNIC: {employee.CNIC}");
            sb.AppendLine($"Phone: {employee.PhoneNumber}");
            sb.AppendLine($"Email: {employee.Email ?? "Not provided"}");
            sb.AppendLine($"Campus: {employee.Campus?.Name}");
            sb.AppendLine($"Address: {employee.Address}");
            sb.AppendLine($"Joining Date: {employee.JoiningDate:dd MMM yyyy}");

            return sb.ToString();
        }

        private async Task<string> GetClassListAsync(ChatUserContext ctx)
        {
            var classes = await _context.Classes
                .Where(c => c.IsActive)
                .Include(c => c.ClassSections)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Available Classes");
            sb.AppendLine();

            foreach (var cls in classes)
            {
                var sections = cls.ClassSections?.Where(s => s.IsActive).Select(s => s.Name).ToList() ?? new List<string>();
                var sectionStr = sections.Any() ? $" (Sections: {string.Join(", ", sections)})" : "";
                sb.AppendLine($"  {cls.Name}{sectionStr}");
            }

            return sb.ToString();
        }

        private async Task<string> GetExamListAsync(ChatUserContext ctx)
        {
            var categories = await _context.Set<ExamCategory>()
                .Where(c => c.IsActive)
                .ToListAsync();

            var exams = await _context.Set<Exam>()
                .Include(e => e.ExamCategory)
                .Where(e => e.IsActive)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Available Exams");
            sb.AppendLine();

            var grouped = exams.GroupBy(e => e.ExamCategory?.Name ?? "Uncategorized").ToList();
            foreach (var group in grouped)
            {
                sb.AppendLine($"{group.Key}:");
                foreach (var exam in group)
                {
                    sb.AppendLine($"  - {exam.Name}");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        // ???????????????????????????????????????????????????????????????????????
        // GENERAL RESPONSE
        // ???????????????????????????????????????????????????????????????????????

        private async Task<string> GetGeneralResponseTextAsync(ChatIntent intent, ChatUserContext ctx)
        {
            return "I can help you with:\n\n" +
                   "ATTENDANCE:\n" +
                   "  - 'Attendance of [student name]'\n" +
                   "  - 'Class 9 attendance' or 'Nine class attendance'\n" +
                   "  - 'Class 9 Section A attendance'\n" +
                   "  - 'Employee attendance' or 'Teacher attendance'\n" +
                   "  - 'Attendance of [employee name]'\n" +
                   "  - Add date: 'attendance on 15 Jan' or 'yesterday attendance'\n\n" +
                   "EXAM RESULTS:\n" +
                   "  - 'Marks of [student name]'\n" +
                   "  - 'Marks of [name] in T1' (specific exam)\n" +
                   "  - 'Marks of [name] in Mid Term' (exam category)\n" +
                   "  - 'Class 10 marks' or 'Class 10 Section B performance'\n" +
                   "  - 'My marks' (for students)\n\n" +
                   "STATISTICS:\n" +
                   "  - 'How many students?' or 'Student count'\n" +
                   "  - 'How many teachers?' or 'Employee count'\n" +
                   "  - 'Student info [name]' or 'Employee info [name]'\n" +
                   "  - 'List of classes' or 'Available exams'\n\n" +
                   "Try asking one of these!";
        }

        private async Task<ChatResponse> GetGeneralResponseAsync(string message, List<ChatMessage> conversationHistory, ChatUserContext ctx)
        {
            try
            {
                var apiKey = _configuration["AiChat:GroqApiKey"];
                var apiUrl = _configuration["AiChat:GroqApiUrl"] ?? "https://api.groq.com/openai/v1";
                var models = _configuration.GetSection("AiChat:GroqModels").Get<string[]>() ?? new[] { "llama-3.1-8b-instant" };

                if (string.IsNullOrEmpty(apiKey))
                {
                    return new ChatResponse
                    {
                        Success = true,
                        Message = await GetGeneralResponseTextAsync(new ChatIntent { Intent = "general" }, ctx)
                    };
                }

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var systemPrompt = $@"You are a helpful School Management System Assistant. Today is {DateTime.Now:MMMM dd, yyyy}.
User: {ctx.FullName} ({ctx.Role}). Keep responses concise, helpful and professional. 
Do not use emoji characters or special symbols like question marks in sequence.
If the user asks about data you don't have access to, guide them on how to phrase their request for the school database queries.";

                var messages = new List<object>
                {
                    new { role = "system", content = systemPrompt }
                };

                foreach (var msg in conversationHistory.TakeLast(10))
                {
                    messages.Add(new { role = msg.Role, content = msg.Content });
                }
                messages.Add(new { role = "user", content = message });

                foreach (var model in models)
                {
                    try
                    {
                        var requestBody = new
                        {
                            model = model,
                            messages = messages,
                            max_tokens = 1024,
                            temperature = 0.7
                        };

                        var response = await client.PostAsync(
                            $"{apiUrl}/chat/completions",
                            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));

                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            var jsonDoc = JsonDocument.Parse(responseContent);
                            var aiMessage = jsonDoc.RootElement
                                .GetProperty("choices")[0]
                                .GetProperty("message")
                                .GetProperty("content")
                                .GetString();

                            // Clean up any problematic characters
                            aiMessage = CleanResponse(aiMessage ?? "");

                            return new ChatResponse { Success = true, Message = aiMessage };
                        }

                        if ((int)response.StatusCode == 429)
                        {
                            await Task.Delay(500);
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error with model {Model}", model);
                        continue;
                    }
                }

                return new ChatResponse
                {
                    Success = true,
                    Message = await GetGeneralResponseTextAsync(new ChatIntent { Intent = "general" }, ctx)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in general response");
                return new ChatResponse
                {
                    Success = true,
                    Message = await GetGeneralResponseTextAsync(new ChatIntent { Intent = "general" }, ctx)
                };
            }
        }

        // ???????????????????????????????????????????????????????????????????????
        // HELPER METHODS
        // ???????????????????????????????????????????????????????????????????????

        private async Task<int?> ResolveClassIdAsync(string? className)
        {
            if (string.IsNullOrEmpty(className))
                return null;

            // First try direct match
            var classObj = await _context.Classes
                .FirstOrDefaultAsync(c => c.IsActive && c.Name.Contains(className));

            if (classObj != null)
                return classObj.Id;

            // Try mapping variations
            var normalizedClass = NormalizeClassName(className);
            if (!string.IsNullOrEmpty(normalizedClass))
            {
                classObj = await _context.Classes
                    .FirstOrDefaultAsync(c => c.IsActive && c.Name.Contains(normalizedClass));
                if (classObj != null)
                    return classObj.Id;
            }

            // Try all classes and fuzzy match
            var classes = await _context.Classes.Where(c => c.IsActive).ToListAsync();
            var match = classes.FirstOrDefault(c => 
                FuzzyMatch(c.Name, className) || 
                c.Name.Contains(className, StringComparison.OrdinalIgnoreCase));

            return match?.Id;
        }

        private string? NormalizeClassName(string input)
        {
            input = input.Trim().ToLower();

            // Direct number
            if (int.TryParse(input, out _))
                return input;

            // Check mappings
            foreach (var kvp in ClassNameMappings)
            {
                if (kvp.Value.Any(v => input.Contains(v, StringComparison.OrdinalIgnoreCase)))
                    return kvp.Key;
            }

            // Extract number from string like "class9" or "grade 10"
            var numbers = new string(input.Where(char.IsDigit).ToArray());
            if (!string.IsNullOrEmpty(numbers))
                return numbers;

            return null;
        }

        private (DateTime? startDate, DateTime? endDate) GetDateRange(ChatIntent intent)
        {
            DateTime? startDate = null;
            DateTime? endDate = null;

            // Try parsing specific date
            if (!string.IsNullOrEmpty(intent.Date))
            {
                if (DateTime.TryParse(intent.Date, out var date))
                {
                    startDate = date;
                    endDate = date;
                }
            }

            // Handle date range
            if (!string.IsNullOrEmpty(intent.DateRange))
            {
                switch (intent.DateRange.ToLower())
                {
                    case "today":
                        startDate = DateTime.Today;
                        endDate = DateTime.Today;
                        break;
                    case "yesterday":
                        startDate = DateTime.Today.AddDays(-1);
                        endDate = DateTime.Today.AddDays(-1);
                        break;
                    case "last_week":
                        startDate = DateTime.Today.AddDays(-7);
                        endDate = DateTime.Today;
                        break;
                    case "last_month":
                        startDate = DateTime.Today.AddMonths(-1);
                        endDate = DateTime.Today;
                        break;
                    case "this_month":
                        startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                        endDate = DateTime.Today;
                        break;
                }
            }

            return (startDate, endDate);
        }

        private string GetStatusText(string status)
        {
            return status switch
            {
                "P" => "Present",
                "A" => "Absent",
                "L" => "Leave",
                "S" => "Short Leave",
                "T" => "Late",
                _ => status
            };
        }

        private bool FuzzyMatch(string source, string target)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
                return false;

            source = source.ToLower().Trim();
            target = target.ToLower().Trim();

            // Exact match
            if (source == target)
                return true;

            // Contains match
            if (source.Contains(target) || target.Contains(source))
                return true;

            // Calculate similarity using Levenshtein distance
            var distance = LevenshteinDistance(source, target);
            var maxLength = Math.Max(source.Length, target.Length);
            var similarity = 1.0 - (double)distance / maxLength;

            return similarity >= 0.7; // 70% similarity threshold
        }

        private int LevenshteinDistance(string s1, string s2)
        {
            var bounds = new { Height = s1.Length + 1, Width = s2.Length + 1 };
            var matrix = new int[bounds.Height, bounds.Width];

            for (var height = 0; height < bounds.Height; height++)
                matrix[height, 0] = height;
            for (var width = 0; width < bounds.Width; width++)
                matrix[0, width] = width;

            for (var height = 1; height < bounds.Height; height++)
            {
                for (var width = 1; width < bounds.Width; width++)
                {
                    var cost = s1[height - 1] == s2[width - 1] ? 0 : 1;
                    var insertion = matrix[height, width - 1] + 1;
                    var deletion = matrix[height - 1, width] + 1;
                    var substitution = matrix[height - 1, width - 1] + cost;

                    matrix[height, width] = Math.Min(Math.Min(insertion, deletion), substitution);
                }
            }

            return matrix[bounds.Height - 1, bounds.Width - 1];
        }

        private string CleanResponse(string response)
        {
            // Remove multiple question marks
            while (response.Contains("??"))
                response = response.Replace("??", "?");

            // Remove emoji-like Unicode characters
            var cleaned = new StringBuilder();
            foreach (var c in response)
            {
                // Keep standard ASCII and common extended Latin characters
                if (c < 0x1F600 || c > 0x1F9FF) // Emoji range
                {
                    if (c < 0x2600 || c > 0x26FF) // Misc symbols
                    {
                        if (c < 0x2700 || c > 0x27BF) // Dingbats
                        {
                            cleaned.Append(c);
                        }
                    }
                }
            }

            return cleaned.ToString().Trim();
        }
    }

    // ???????????????????????????????????????????????????????????????????????????
    // DTOs
    // ???????????????????????????????????????????????????????????????????????????

    public class ChatIntent
    {
        public string? Intent { get; set; }
        public string? StudentName { get; set; }
        public string? EmployeeName { get; set; }
        public string? ClassName { get; set; }
        public string? SectionName { get; set; }
        public string? ExamCategory { get; set; }
        public string? ExamName { get; set; }
        public string? SubjectName { get; set; }
        public string? Date { get; set; }
        public string? DateRange { get; set; }
        public bool IsMyData { get; set; }
    }

    public class ChatResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ChatMessage
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
    }

    public class ChatUserContext
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Student";
        public int? StudentId { get; set; }
        public int? EmployeeId { get; set; }
        public int? CampusId { get; set; }
    }
}
