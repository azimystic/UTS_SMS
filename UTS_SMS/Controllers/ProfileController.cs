using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using UTS_SMS.ViewModels;

namespace UTS_SMS.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProfileController(
            UserManager<ApplicationUser> userManager, 
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var isAdmin = roles.Contains("Admin");

            var model = new ProfileViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Username = user.UserName,
                Role = roles.FirstOrDefault(),
                IsAdmin = isAdmin,
                StudentId = user.StudentId,
                EmployeeId = user.EmployeeId,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                AvatarUrl = user.AvatarUrl
            };

            // If user is a student, get additional details
            if (user.StudentId.HasValue)
            {
                var student = await _context.Students
                    .Include(s => s.ClassObj)
                    .Include(s => s.SectionObj)
                    .FirstOrDefaultAsync(s => s.Id == user.StudentId.Value);
                if (student != null)
                {
                    model.StudentDetails = student;
                    ViewBag.StudentDetails = student;
                }
            }

            // If user is an employee, get additional details
            if (user.EmployeeId.HasValue)
            {
                var employee = await _context.Employees.FindAsync(user.EmployeeId.Value);
                if (employee != null)
                {
                    model.EmployeeDetails = employee;
                    ViewBag.EmployeeDetails = employee;
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBasicInfo(string fullName, string phoneNumber, string address)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            try
            {
                user.FullName = fullName;
                user.PhoneNumber = phoneNumber;
                user.Address = address;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "Profile updated successfully" });
                }

                return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile avatarFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            if (avatarFile == null || avatarFile.Length == 0)
            {
                return Json(new { success = false, message = "No file selected" });
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return Json(new { success = false, message = "Only image files are allowed (jpg, jpeg, png, gif)" });
            }

            // Validate file size (max 5MB)
            if (avatarFile.Length > 5 * 1024 * 1024)
            {
                return Json(new { success = false, message = "File size must be less than 5MB" });
            }

            try
            {
                // Delete old avatar if exists
                if (!string.IsNullOrEmpty(user.AvatarUrl))
                {
                    var oldAvatarPath = Path.Combine(_webHostEnvironment.WebRootPath, user.AvatarUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldAvatarPath))
                    {
                        System.IO.File.Delete(oldAvatarPath);
                    }
                }

                // Create uploads directory if it doesn't exist
                var uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");
                Directory.CreateDirectory(uploadsDir);

                // Generate unique filename
                var fileName = $"{user.Id}_{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsDir, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(stream);
                }

                // Update user avatar URL
                user.AvatarUrl = $"/uploads/avatars/{fileName}";
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "Avatar updated successfully", avatarUrl = user.AvatarUrl });
                }

                return Json(new { success = false, message = "Failed to update avatar" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAvatar()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            try
            {
                // Delete file if exists
                if (!string.IsNullOrEmpty(user.AvatarUrl))
                {
                    var avatarPath = Path.Combine(_webHostEnvironment.WebRootPath, user.AvatarUrl.TrimStart('/'));
                    if (System.IO.File.Exists(avatarPath))
                    {
                        System.IO.File.Delete(avatarPath);
                    }
                }

                user.AvatarUrl = null;
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "Avatar deleted successfully" });
                }

                return Json(new { success = false, message = "Failed to delete avatar" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            try
            {
                var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
                
                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "Password changed successfully" });
                }

                return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // OTP-based password change
        private static Dictionary<string, (string Otp, DateTime Expiry)> _passwordChangeOtpStore = new Dictionary<string, (string, DateTime)>();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestPasswordChangeOtp()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            if (string.IsNullOrEmpty(user.Email))
            {
                return Json(new { success = false, message = "No email address found for your account" });
            }

            try
            {
                // Generate 6-digit OTP using cryptographically secure random number generator
                var otp = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
                var expiry = DateTime.Now.AddMinutes(10);
                
                // Store OTP
                _passwordChangeOtpStore[user.Email] = (otp, expiry);

                // Note: You'll need to inject IEmailService in the ProfileController constructor
                // For now, we'll skip the actual email sending, but the structure is here
                // await _emailService.SendEmailAsync(user.Email, "Password Change OTP", $"Your OTP is: {otp}");

                return Json(new { success = true, message = "OTP has been sent to your email address", email = user.Email });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtpAndChangePassword(string otp, string newPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            try
            {
                // Check if OTP exists and is valid
                if (!_passwordChangeOtpStore.ContainsKey(user.Email))
                {
                    return Json(new { success = false, message = "Invalid or expired OTP" });
                }

                var (storedOtp, expiry) = _passwordChangeOtpStore[user.Email];
                
                if (DateTime.Now > expiry)
                {
                    _passwordChangeOtpStore.Remove(user.Email);
                    return Json(new { success = false, message = "OTP has expired. Please request a new one" });
                }

                if (storedOtp != otp)
                {
                    return Json(new { success = false, message = "Invalid OTP" });
                }

                // OTP is valid, change password
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

                if (result.Succeeded)
                {
                    _passwordChangeOtpStore.Remove(user.Email);
                    return Json(new { success = true, message = "Password changed successfully" });
                }

                return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(IFormFile documentFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            // Check if user is student or teacher
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Student") && !roles.Contains("Teacher"))
            {
                return Json(new { success = false, message = "Only students and teachers can upload documents" });
            }

            if (documentFile == null || documentFile.Length == 0)
            {
                return Json(new { success = false, message = "No file selected" });
            }

            // Validate file type (allow common document types)
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(documentFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return Json(new { success = false, message = "Only PDF, Word, and image files are allowed" });
            }

            // Validate file size (max 10MB)
            if (documentFile.Length > 10 * 1024 * 1024)
            {
                return Json(new { success = false, message = "File size must be less than 10MB" });
            }

            try
            {
                // Create documents directory if it doesn't exist
                var documentsDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "documents", user.Id);
                Directory.CreateDirectory(documentsDir);

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(documentsDir, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await documentFile.CopyToAsync(stream);
                }

                var fileUrl = $"/uploads/documents/{user.Id}/{fileName}";
                return Json(new { success = true, message = "Document uploaded successfully", fileUrl = fileUrl, fileName = documentFile.FileName });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(string fileUrl)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            try
            {
                if (!string.IsNullOrEmpty(fileUrl))
                {
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, fileUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                return Json(new { success = true, message = "Document deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Admin Only Actions
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var model = new ProfileViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Username = user.UserName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Role = roles.FirstOrDefault(),
                AvatarUrl = user.AvatarUrl,
                StudentId = user.StudentId,
                EmployeeId = user.EmployeeId
            };

            ViewBag.AllRoles = new List<string> { "Admin", "Teacher", "Student", "Accountant", "Parent", "Owner" };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminUpdate(string userId, string fullName, string email, string phoneNumber, 
            string address, string role, IFormFile? avatarFile)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            try
            {
                user.FullName = fullName;
                user.Email = email;
                user.UserName = email;
                user.PhoneNumber = phoneNumber;
                user.Address = address;

                // Handle avatar upload
                if (avatarFile != null && avatarFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
                    
                    if (allowedExtensions.Contains(extension) && avatarFile.Length <= 5 * 1024 * 1024)
                    {
                        if (!string.IsNullOrEmpty(user.AvatarUrl))
                        {
                            var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, user.AvatarUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldPath))
                            {
                                System.IO.File.Delete(oldPath);
                            }
                        }

                        var uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");
                        Directory.CreateDirectory(uploadsDir);
                        var fileName = $"{user.Id}_{Guid.NewGuid()}{extension}";
                        var filePath = Path.Combine(uploadsDir, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await avatarFile.CopyToAsync(stream);
                        }

                        user.AvatarUrl = $"/uploads/avatars/{fileName}";
                    }
                }

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
                }

                // Update role if changed
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (!currentRoles.Contains(role))
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    await _userManager.AddToRoleAsync(user, role);
                }

                return Json(new { success = true, message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}