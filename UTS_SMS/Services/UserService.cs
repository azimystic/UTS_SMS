using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;

namespace UTS_SMS.Services
{
    public interface IUserService
    {
        Task<IdentityResult> CreateStudentUserAsync(Student student);
        Task<IdentityResult> CreateEmployeeUserAsync(Employee employee);
        Task<IdentityResult> CreateAccountantUserAsync(Employee employee);
        Task<IdentityResult> CreateAdminUserAsync(string email, string fullName);
        Task<IdentityResult> CreateOwnerUserAsync(string email, string fullName);
        Task<IdentityResult> CreateParentUserAsync(Family family);
        Task<string> GeneratePasswordAsync();
        Task InitializeRolesAsync();
        Task<bool> UpdateEmployeeUsernameAsync(int employeeId, string newCNIC);
     }

    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;

        public UserService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IEmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
        }

        public async Task InitializeRolesAsync()
        {
            string[] roles = { "Admin", "Teacher", "Student", "Owner", "Accountant", "Parent" };

            foreach (string role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        public async Task<IdentityResult> CreateStudentUserAsync(Student student)
        {
            // Use default password for all students
            var password = "Pass@123";

            // Use CNIC without dashes as username
            var username = student.StudentCNIC?.Replace("-", "") ?? GenerateUsername(student.StudentName, student.Id);
            
            // Use email from student form, or generate if not provided
            var email = !string.IsNullOrWhiteSpace(student.Email) 
                ? student.Email 
                : GenerateEmail(student.StudentName, student.Id, "student");

            var user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                FullName = student.StudentName,
                StudentId = student.Id,
                EmailConfirmed = true,
                IsActive = true,
                CampusId = student.CampusId
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Student");

                // Store password in Student model for initial communication
                student.RegisteredBy += $" | User: {user.UserName} | Pass: {password}";
                
                // Send welcome email
                await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName, user.UserName, password, "Student");
            }

            return result;
        }

        public async Task<IdentityResult?> CreateEmployeeUserAsync(Employee employee)
        {
            // Only create a user if role is Teacher
            if (employee.Role?.ToLower() != "teacher")
            {
                return IdentityResult.Success;
            }

            // Use Employee CNIC as password (without dashes)
            var password =  "Teacher@123";
            var role = "Teacher";

            // Use CNIC without dashes as username
            var username = employee.CNIC?.Replace("-", "") ?? GenerateUsername(employee.FullName, employee.Id);
            
            // Use email from employee form, or generate if not provided
            var email = !string.IsNullOrWhiteSpace(employee.Email) 
                ? employee.Email 
                : GenerateEmail(employee.FullName, employee.Id, "employee");

            var user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                FullName = employee.FullName,
                EmployeeId = employee.Id,
                CampusId = employee.CampusId,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);

                // Store password in Employee model for initial communication
                employee.RegisteredBy += $" | User: {user.UserName} | Pass: {password}";
                
                // Send welcome email
                await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName, user.UserName, password, role);
            }

            return result;
        }

        public async Task<IdentityResult?> CreateAccountantUserAsync(Employee employee)
        {
            // Only create a user if role is accountant
            if (employee.Role?.ToLower() != "accountant")
            {
                return IdentityResult.Success;
            }

            // Use Employee CNIC as password (without dashes)
            var password = employee.CNIC?.Replace("-", "") ?? "Accountant@123";
            var role = "Accountant";

            // Use CNIC without dashes as username
            var username = employee.CNIC?.Replace("-", "") ?? GenerateUsername(employee.FullName, employee.Id);
            
            // Use email from employee form, or generate if not provided
            var email = !string.IsNullOrWhiteSpace(employee.Email) 
                ? employee.Email 
                : GenerateEmail(employee.FullName, employee.Id, "employee");

            var user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                FullName = employee.FullName,
                EmployeeId = employee.Id,
                CampusId = employee.CampusId,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);

                // Store password in Employee model for initial communication
                employee.RegisteredBy += $" | User: {user.UserName} | Pass: {password}";
                
                // Send welcome email
                await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName, user.UserName, password, role);
            }

            return result;
        }


        public async Task<IdentityResult> CreateAdminUserAsync(string email, string fullName)
        {
            var password = "Admin@123";

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);
            
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Admin");
                // ✅ Log the generated password
                Console.WriteLine($"Default Admin created. Email: {email} | Password: {password}");
            }

            return result;
        }
        public async Task<IdentityResult> CreateOwnerUserAsync(string email, string fullName)
        {
            var password = "Admin@123";

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Owner");
              }

            return result;
        }

        public async Task<IdentityResult> CreateParentUserAsync(Family family)
        {
            // Use Father CNIC as password (without dashes)
            var password = family.FatherCNIC?.Replace("-", "") ?? "Parent@123";

            // Use CNIC without dashes as username
            var username = family.FatherCNIC?.Replace("-", "") ?? GenerateParentUsername(family.FatherCNIC, family.Id);
            
            // Use email from family form, or generate if not provided
            var email = !string.IsNullOrWhiteSpace(family.Email) 
                ? family.Email 
                : GenerateParentEmail(family.FatherCNIC, family.Id);
            
            var user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                FullName = family.FatherName,
                EmailConfirmed = true,
                CampusId = family.CampusId,
                FamilyId = family.Id,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Parent");
                
                // Send welcome email
                await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName, user.UserName, password, "Parent");
            }

            return result;
        }

        public async Task<string> GeneratePasswordAsync()
        {
            // Generate a secure 8-character password
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var random = new Random();
            var password = new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray()) + "!";

            return await Task.FromResult(password);
        }

        private string GenerateUsername(string name, int id)
        {
            // Remove spaces and special characters, take first 6 characters + ID
            var cleanName = new string(name.Where(c => char.IsLetterOrDigit(c)).ToArray()).ToLower();
            return cleanName.Length > 6 ? cleanName.Substring(0, 6) + id : cleanName + id;
        }

        private string GenerateEmail(string name, int id, string type)
        {
            var username = GenerateUsername(name, id);
            return $"{username}@{type}.smartschool.edu.pk";
        }

        private string GenerateParentUsername(string fatherCNIC, int familyId)
        {
            // Use last 4 digits of CNIC + family ID for parent username
            var cnicSuffix = fatherCNIC.Substring(fatherCNIC.Length - 4);
            return $"parent{cnicSuffix}{familyId}";
        }

        private string GenerateParentEmail(string fatherCNIC, int familyId)
        {
            var username = GenerateParentUsername(fatherCNIC, familyId);
            return $"{username}@parent.smartschool.edu.pk";
        }

        public async Task<bool> UpdateEmployeeUsernameAsync(int employeeId, string newCNIC)
        {
            // Find the user associated with this employee
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
            
            if (user == null)
            {
                return false; // No user account exists for this employee
            }

            // Generate new username from CNIC
            var newUsername = newCNIC?.Replace("-", "") ?? user.UserName;
            
            if (newUsername == user.UserName)
            {
                return true; // No change needed
            }

            // Update username
            user.UserName = newUsername;
            user.NormalizedUserName = newUsername.ToUpper();
            
            var result = await _userManager.UpdateAsync(user);
            
            return result.Succeeded;
        }
    }
}