using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;

namespace SMS.Services
{
    public interface IDatabaseSeeder
    {
        Task SeedSampleDataAsync();
        Task ClearAllDataAsync();
    }

    public class DatabaseSeeder : IDatabaseSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly ILogger<DatabaseSeeder> _logger;
        private readonly Random _random = new();

        public DatabaseSeeder(ApplicationDbContext context, IUserService userService, ILogger<DatabaseSeeder> logger)
        {
            _context = context;
            _userService = userService;
            _logger = logger;
        }

        public async Task SeedSampleDataAsync()
        {
            try
            {
                _logger.LogInformation("Starting sample data seeding...");

                // Check if data already exists
                if (await _context.Students.AnyAsync())
                {
                    _logger.LogWarning("Sample data already exists. Skipping seeding.");
                    return;
                }

                // Seed in order of dependencies
                await SeedCampusesAsync();
                await SeedClassesAsync();
                await SeedSectionsAsync();
                await SeedSubjectsAsync();
                await SeedSubjectsGroupingsAsync();
                await SeedExamCategoriesAsync();
                await SeedExamsAsync();
                await SeedFamiliesAsync();
                await SeedStudentsAsync();
                await SeedEmployeesAsync();
                await SeedClassFeesAsync();
                await SeedBankAccountsAsync();

                _logger.LogInformation("Sample data seeding completed successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding sample data");
                throw;
            }
        }

        private async Task SeedCampusesAsync()
        {
            var campuses = new List<Campus>
            {
                new Campus { Name = "Main Campus", Code = "MC01", Address = "123 Main Street, City", Phone = "0300-1234567", Email = "main@school.edu", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(14, 0), Latitudes = "31.5204", Longitudes = "74.3587" },
                new Campus { Name = "North Campus", Code = "NC01", Address = "456 North Ave, City", Phone = "0300-1234568", Email = "north@school.edu", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(14, 0), Latitudes = "31.5304", Longitudes = "74.3687" },
                new Campus { Name = "South Campus", Code = "SC01", Address = "789 South Blvd, City", Phone = "0300-1234569", Email = "south@school.edu", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(14, 0), Latitudes = "31.5104", Longitudes = "74.3487" }
            };

            await _context.Campuses.AddRangeAsync(campuses);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} campuses", campuses.Count);
        }

        private async Task SeedClassesAsync()
        {
            var campuses = await _context.Campuses.ToListAsync();
            var classes = new List<Class>();
            var classNames = new[] { "Nursery", "Prep", "Class 1", "Class 2", "Class 3", "Class 4", "Class 5", "Class 6", "Class 7", "Class 8", "Class 9", "Class 10" };

            foreach (var campus in campuses)
            {
                foreach (var className in classNames)
                {
                    classes.Add(new Class
                    {
                        Name = className,
                        GradeLevel = className,
                        CampusId = campus.Id,
                        IsActive = true
                    });
                }
            }

            await _context.Classes.AddRangeAsync(classes);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} classes", classes.Count);
        }

        private async Task SeedSectionsAsync()
        {
            var classes = await _context.Classes.ToListAsync();
            var sections = new List<ClassSection>();
            var sectionNames = new[] { "A", "B", "C", "D" };

            foreach (var classItem in classes)
            {
                foreach (var sectionName in sectionNames)
                {
                    sections.Add(new ClassSection
                    {
                        Name = sectionName,
                        ClassId = classItem.Id,
                        CampusId = classItem.CampusId,
                        Capacity = 30,
                        IsActive = true
                    });
                }
            }

            await _context.ClassSections.AddRangeAsync(sections);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} sections", sections.Count);
        }

        private async Task SeedSubjectsAsync()
        {
            var campuses = await _context.Campuses.ToListAsync();
            var subjects = new List<Subject>();
            var subjectNames = new[] { "English", "Urdu", "Mathematics", "Science", "Islamiat", "Pakistan Studies", "Computer Science", "Physics", "Chemistry", "Biology" };

            foreach (var campus in campuses)
            {
                foreach (var subjectName in subjectNames)
                {
                    subjects.Add(new Subject
                    {
                        Name = subjectName,
                        Code = subjectName.Substring(0, 3).ToUpper(),
                        Description = $"{subjectName} subject",
                        CampusId = campus.Id,
                        IsActive = true
                    });
                }
            }

            await _context.Subjects.AddRangeAsync(subjects);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} subjects", subjects.Count);
        }

        private async Task SeedSubjectsGroupingsAsync()
        {
            var campuses = await _context.Campuses.ToListAsync();
            var subjects = await _context.Subjects.ToListAsync();
            var groupings = new List<SubjectsGrouping>();

            foreach (var campus in campuses)
            {
                var campusSubjects = subjects.Where(s => s.CampusId == campus.Id).ToList();
                
                var scienceGroup = new SubjectsGrouping
                {
                    Name = "Science Group",
                    CampusId = campus.Id,
                    IsActive = true
                };
                groupings.Add(scienceGroup);

                var artsGroup = new SubjectsGrouping
                {
                    Name = "Arts Group",
                    CampusId = campus.Id,
                    IsActive = true
                };
                groupings.Add(artsGroup);
            }

            await _context.SubjectsGroupings.AddRangeAsync(groupings);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} subject groupings", groupings.Count);
        }

        private async Task SeedExamCategoriesAsync()
        {
            var campuses = await _context.Campuses.ToListAsync();
            var categories = new List<ExamCategory>();
            var categoryNames = new[] { "Monthly Test", "Mid Term", "Final Term", "Class Test" };

            foreach (var campus in campuses)
            {
                foreach (var categoryName in categoryNames)
                {
                    categories.Add(new ExamCategory
                    {
                        Name = categoryName,
                        CampusId = campus.Id,
                        IsActive = true
                    });
                }
            }

            await _context.ExamCategories.AddRangeAsync(categories);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} exam categories", categories.Count);
        }

        private async Task SeedExamsAsync()
        {
            var categories = await _context.ExamCategories.Include(c => c.Campus).ToListAsync();
            var exams = new List<Exam>();

            foreach (var category in categories)
            {
                for (int i = 1; i <= 3; i++)
                {
                    exams.Add(new Exam
                    {
                        Name = $"{category.Name} - {i}",
                        ExamCategoryId = category.Id,
                        CampusId = category.CampusId,
                        IsActive = true
                    });
                }
            }

            await _context.Exams.AddRangeAsync(exams);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} exams", exams.Count);
        }

        private async Task SeedFamiliesAsync()
        {
            var campuses = await _context.Campuses.ToListAsync();
            var families = new List<Family>();
            var firstNames = new[] { "Ahmed", "Ali", "Usman", "Hassan", "Bilal", "Kamran", "Farhan", "Imran", "Zain", "Adnan" };
            var lastNames = new[] { "Khan", "Ahmed", "Ali", "Shah", "Malik", "Hussain", "Raza", "Hassan", "Siddiqui", "Akhtar" };

            for (int i = 1; i <= 100; i++)
            {
                var campus = campuses[_random.Next(campuses.Count)];
                var firstName = firstNames[_random.Next(firstNames.Length)];
                var lastName = lastNames[_random.Next(lastNames.Length)];

                families.Add(new Family
                {
                    FatherName = $"{firstName} {lastName}",
                    FatherCNIC = $"35202{_random.Next(1000000, 9999999)}",
                    FatherPhone = $"0300{_random.Next(1000000, 9999999)}",
                    MotherName = $"Fatima {lastName}",
                    MotherCNIC = $"35202{_random.Next(1000000, 9999999)}",
                    MotherPhone = $"0301{_random.Next(1000000, 9999999)}",
                    HomeAddress = $"House {i}, Street {_random.Next(1, 50)}, Area {_random.Next(1, 20)}, City",
                    FatherSourceOfIncome = new[] { "Business", "Job", "Professional", "Self-Employed" }[_random.Next(4)],
                    CampusId = campus.Id,
                    IsActive = true,
                    CreatedBy = "Seeder",
                    CreatedDate = DateTime.Now.AddDays(-_random.Next(1, 365))
                });
            }

            await _context.Families.AddRangeAsync(families);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} families", families.Count);

            // Create parent users for families
            foreach (var family in families)
            {
                try
                {
                    await _userService.CreateParentUserAsync(family);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create parent user for family {FamilyId}", family.Id);
                }
            }
        }

        private async Task SeedStudentsAsync()
        {
            var families = await _context.Families.ToListAsync();
            var classes = await _context.Classes.Include(c => c.ClassSections).ToListAsync();
            var groupings = await _context.SubjectsGroupings.ToListAsync();
            var students = new List<Student>();

            var boyNames = new[] { "Ahmed", "Ali", "Usman", "Hassan", "Bilal", "Zain", "Hamza", "Abdullah", "Ibrahim", "Talha" };
            var girlNames = new[] { "Fatima", "Aisha", "Maryam", "Zainab", "Hafsa", "Amina", "Khadija", "Sara", "Noor", "Laiba" };

            for (int i = 1; i <= 100; i++)
            {
                var family = families[_random.Next(families.Count)];
                var classItem = classes[_random.Next(classes.Count)];
                var section = classItem.ClassSections.ElementAt(_random.Next(classItem.ClassSections.Count));
                var grouping = groupings.FirstOrDefault(g => g.CampusId == classItem.CampusId);
                var gender = _random.Next(2) == 0 ? "Male" : "Female";
                var name = gender == "Male" ? boyNames[_random.Next(boyNames.Length)] : girlNames[_random.Next(girlNames.Length)];

                var student = new Student
                {
                    StudentName = $"{name} {family.FatherName.Split(' ').Last()}",
                    FatherName = family.FatherName,
                    FatherCNIC = family.FatherCNIC,
                    FatherPhone = family.FatherPhone,
                    MotherName = family.MotherName,
                    MotherCNIC = family.MotherCNIC,
                    MotherPhone = family.MotherPhone,
                    StudentCNIC = $"35202{_random.Next(1000000, 9999999)}",
                    DateOfBirth = DateTime.Now.AddYears(-_random.Next(5, 18)),
                    Gender = gender,
                    HomeAddress = family.HomeAddress,
                    Class = classItem.Id,
                    Section = section.Id,
                    CampusId = classItem.CampusId,
                    FamilyId = family.Id,
                    SubjectsGroupingId = grouping?.Id ?? 1,
                    RegistrationDate = DateTime.Now.AddDays(-_random.Next(1, 365)),
                    RegisteredBy = "Seeder",
                    HasLeft = false,
                    AdmissionFeePaid = _random.Next(2) == 0,
                    FatherSourceOfIncome = family.FatherSourceOfIncome
                };

                students.Add(student);
            }

            await _context.Students.AddRangeAsync(students);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} students", students.Count);

            // Create student users
            foreach (var student in students)
            {
                try
                {
                    await _userService.CreateStudentUserAsync(student);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create user for student {StudentId}", student.Id);
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedEmployeesAsync()
        {
            var campuses = await _context.Campuses.ToListAsync();
            var employees = new List<Employee>();

            var teacherNames = new[] { "Dr. Ahmed Hassan", "Prof. Fatima Khan", "Mr. Ali Raza", "Ms. Sara Ahmed", "Mr. Usman Malik" };
            var roles = new[] { "Teacher", "Teacher", "Teacher", "Accountant", "Admin", "Aya", "Guard" };

            for (int i = 1; i <= 100; i++)
            {
                var campus = campuses[_random.Next(campuses.Count)];
                var role = roles[_random.Next(roles.Length)];
                var name = i <= 50 ? $"Teacher {i}" : $"Staff {i}";

                var employee = new Employee
                {
                    FullName = name,
                    CNIC = $"35202{_random.Next(1000000, 9999999)}",
                    PhoneNumber = $"0300{_random.Next(1000000, 9999999)}",
                    Address = $"House {i}, Street {_random.Next(1, 50)}, City",
                    DateOfBirth = DateTime.Now.AddYears(-_random.Next(25, 55)),
                    Gender = _random.Next(2) == 0 ? "Male" : "Female",
                    Role = role,
                    JoiningDate = DateTime.Now.AddDays(-_random.Next(1, 1825)),
                    CampusId = campus.Id,
                    IsActive = true,
                    RegisteredBy = "Seeder",
                    EducationLevel = "Bachelor",
                    Degree = "BS",
                    OnTime = new TimeOnly(8, 0),
                    OffTime = new TimeOnly(14, 0),
                    LateTimeFlexibility = 15
                };

                employees.Add(employee);
            }

            await _context.Employees.AddRangeAsync(employees);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} employees", employees.Count);

            // Create employee users for teachers and accountants
            foreach (var employee in employees.Where(e => e.Role == "Teacher"))
            {
                try
                {
                    await _userService.CreateEmployeeUserAsync(employee);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create user for employee {EmployeeId}", employee.Id);
                }
            }

            foreach (var employee in employees.Where(e => e.Role == "Accountant"))
            {
                try
                {
                    await _userService.CreateAccountantUserAsync(employee);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create user for accountant {EmployeeId}", employee.Id);
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedClassFeesAsync()
        {
            var classes = await _context.Classes.ToListAsync();
            var classFees = new List<ClassFee>();

            foreach (var classItem in classes)
            {
                classFees.Add(new ClassFee
                {
                    ClassId = classItem.Id,
                    CampusId = classItem.CampusId,
                    TuitionFee = _random.Next(2000, 5000),
                    AdmissionFee = _random.Next(5000, 10000),
                    CreatedBy = "Seeder",
                    CreatedDate = DateTime.Now,
                    ModifiedBy = "Seeder",
                    ModifiedDate = DateTime.Now
                });
            }

            await _context.ClassFees.AddRangeAsync(classFees);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} class fees", classFees.Count);
        }

        private async Task SeedBankAccountsAsync()
        {
            var campuses = await _context.Campuses.ToListAsync();
            var accounts = new List<BankAccount>();

            foreach (var campus in campuses)
            {
                accounts.Add(new BankAccount
                {
                    BankName = "Meezan Bank",
                    AccountNumber = $"01{_random.Next(10000000, 99999999)}",
                    AccountTitle = $"{campus.Name} School Account",
                    Branch = "Main Branch",
                    BranchCode = "0001",
                    CampusId = campus.Id,
                    IsActive = true
                });

                accounts.Add(new BankAccount
                {
                    BankName = "HBL",
                    AccountNumber = $"02{_random.Next(10000000, 99999999)}",
                    AccountTitle = $"{campus.Name} School Account",
                    Branch = "City Branch",
                    BranchCode = "0002",
                    CampusId = campus.Id,
                    IsActive = true
                });
            }

            await _context.BankAccounts.AddRangeAsync(accounts);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} bank accounts", accounts.Count);
        }

        public async Task ClearAllDataAsync()
        {
            _logger.LogWarning("Clearing all data from database...");

            // Delete in reverse order of dependencies
            _context.BillingTransactions.RemoveRange(_context.BillingTransactions);
            _context.BillingMaster.RemoveRange(_context.BillingMaster);
            _context.StudentFineCharges.RemoveRange(_context.StudentFineCharges);
            _context.ExamMarks.RemoveRange(_context.ExamMarks);
            _context.Attendance.RemoveRange(_context.Attendance);
            _context.Students.RemoveRange(_context.Students);
            _context.Families.RemoveRange(_context.Families);
            _context.EmployeeAttendance.RemoveRange(_context.EmployeeAttendance);
            _context.Employees.RemoveRange(_context.Employees);
            _context.ClassFees.RemoveRange(_context.ClassFees);
            _context.BankAccounts.RemoveRange(_context.BankAccounts);
            _context.Exams.RemoveRange(_context.Exams);
            _context.ExamCategories.RemoveRange(_context.ExamCategories);
            _context.SubjectsGroupings.RemoveRange(_context.SubjectsGroupings);
            _context.Subjects.RemoveRange(_context.Subjects);
            _context.ClassSections.RemoveRange(_context.ClassSections);
            _context.Classes.RemoveRange(_context.Classes);
            _context.Campuses.RemoveRange(_context.Campuses);

            await _context.SaveChangesAsync();
            _logger.LogInformation("All data cleared successfully");
        }
    }
}
