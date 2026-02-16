using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UTS_SMS.Models;
using UTS_SMS.Services;
using System.ComponentModel.DataAnnotations;

namespace UTS_SMS.Controllers
{
     public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;
        private static Dictionary<string, (string Otp, DateTime Expiry)> _otpStore = new Dictionary<string, (string, DateTime)>();

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IUserService userService,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userService = userService;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if user exists and is active
                var user = await _userManager.FindByNameAsync(model.Username);
                if (user != null && !user.IsActive)
                {
                    ModelState.AddModelError("", "Your account has been deactivated. Please contact the administrator.");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, false);

                if (result.Succeeded)
                {
                    var roles = await _userManager.GetRolesAsync(user);

                    // Redirect based on role
                    if (roles.Contains("Admin"))
                        return RedirectToAction("Dashboard", "Admin");
                    else if (roles.Contains("Teacher"))
                        return RedirectToAction("Dashboard", "Teacher");
                    else if (roles.Contains("Owner"))
                        return RedirectToAction("Dashboard", "Owner");
                    else if (roles.Contains("Student"))
                        return RedirectToAction("Dashboard", "StudentDashboard");
                    else if (roles.Contains("Parent"))
                        return RedirectToAction("Dashboard", "ParentDashboard");
                }

                ModelState.AddModelError("", "Invalid login attempt.");
            }

            return View(model);
        }


        // Update the RegisterViewModel class
        public class RegisterViewModel
        {
            [Required]
            [Display(Name = "Full Name")]
            public string FullName { get; set; }

            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            public string Username { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm Password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            [Required]
            public string Role { get; set; }

            public SelectList RoleList { get; set; }
        }
        public class UserWithRolesViewModel
        {
            public ApplicationUser User { get; set; }
            public List<string> Roles { get; set; }
        }
        // Update the Register action methods
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Register()
        {
            var model = new RegisterViewModel
            {
                RoleList = new SelectList(new List<string> { "Admin", "Accountant" })
            };
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            ModelState.Remove(nameof(RegisterViewModel.RoleList));
            if (ModelState.IsValid)
            {
                // Validate role is only Admin or Accountant
                if (model.Role != "Admin" && model.Role != "Accountant")
                {
                    ModelState.AddModelError("", "Only Admin and Accountant roles are allowed.");
                    model.RoleList = new SelectList(new List<string> { "Admin", "Accountant" });
                    return View(model);
                }

                // Create the user
                var user = new ApplicationUser
                {
                    UserName = model.Username,
                    Email = model.Email,
                    FullName = model.FullName,
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Add to selected role
                    await _userManager.AddToRoleAsync(user, model.Role);

                    TempData["Success"] = $"User {model.Username} created successfully!";
                    return RedirectToAction("Users");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            model.RoleList = new SelectList(new List<string> { "Admin", "Accountant" });
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Users()
        {
            var users = _userManager.Users.ToList();
            var userRoles = new List<UserWithRolesViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles.Add(new UserWithRolesViewModel
                {
                    User = user,
                    Roles = roles.ToList()
                });
            }

            return View(userRoles);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeactivateUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.IsActive = false;
                await _userManager.UpdateAsync(user);
                TempData["Success"] = "User deactivated successfully!";
            }

            return RedirectToAction("Users");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReactivateUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.IsActive = true;
                await _userManager.UpdateAsync(user);
                TempData["Success"] = "User reactivated successfully!";
            }

            return RedirectToAction("Users");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError("", "Please enter your email address.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Don't reveal that the user doesn't exist
                TempData["Success"] = "If the email exists, an OTP has been sent to your email address.";
                return RedirectToAction("VerifyOtp");
            }

            // Generate 6-digit OTP
            var otp = new Random().Next(100000, 999999).ToString();
            var expiry = DateTime.Now.AddMinutes(10);
            
            // Store OTP
            _otpStore[email] = (otp, expiry);

            // Send OTP via email
            var subject = "Password Reset OTP - SMS";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .otp-box {{ background: white; padding: 20px; border: 2px dashed #667eea; text-align: center; margin: 20px 0; border-radius: 10px; }}
        .otp {{ font-size: 32px; font-weight: bold; color: #667eea; letter-spacing: 8px; }}
        .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 Password Reset Request</h1>
        </div>
        <div class='content'>
            <h2>Hello {user.FullName}!</h2>
            <p>You have requested to reset your password. Please use the OTP below to verify your identity.</p>
            
            <div class='otp-box'>
                <div style='color: #666; font-size: 14px; margin-bottom: 10px;'>Your One-Time Password</div>
                <div class='otp'>{otp}</div>
            </div>

            <div class='warning'>
                <strong>⚠️ Important:</strong>
                <ul>
                    <li>This OTP is valid for 10 minutes only</li>
                    <li>Do not share this OTP with anyone</li>
                    <li>If you didn't request this, please ignore this email</li>
                </ul>
            </div>

            <p>After verification, your password will be reset to a temporary password which will be displayed to you.</p>
        </div>
    </div>
</body>
</html>";

            await _emailService.SendEmailAsync(email, subject, body);

            TempData["Success"] = "An OTP has been sent to your email address.";
            TempData["Email"] = email;
            return RedirectToAction("VerifyOtp");
        }

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOtp(string email, string otp)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otp))
            {
                ModelState.AddModelError("", "Please enter both email and OTP.");
                return View();
            }

            // Check if OTP exists and is valid
            if (!_otpStore.ContainsKey(email))
            {
                ModelState.AddModelError("", "Invalid or expired OTP. Please request a new one.");
                return View();
            }

            var (storedOtp, expiry) = _otpStore[email];
            
            if (DateTime.Now > expiry)
            {
                _otpStore.Remove(email);
                ModelState.AddModelError("", "OTP has expired. Please request a new one.");
                return View();
            }

            if (storedOtp != otp)
            {
                ModelState.AddModelError("", "Invalid OTP. Please try again.");
                return View();
            }

            // OTP is valid, reset password
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ModelState.AddModelError("", "User not found.");
                return View();
            }

            // Generate a new basic password
            var newPassword = "Pass@123";
            
            // Remove the old password
            var removePasswordResult = await _userManager.RemovePasswordAsync(user);
            if (!removePasswordResult.Succeeded)
            {
                ModelState.AddModelError("", "Failed to reset password. Please try again.");
                return View();
            }

            // Add the new password
            var addPasswordResult = await _userManager.AddPasswordAsync(user, newPassword);
            if (!addPasswordResult.Succeeded)
            {
                ModelState.AddModelError("", "Failed to reset password. Please try again.");
                return View();
            }

            // Remove OTP from store
            _otpStore.Remove(email);

            // Sign in the user
            await _signInManager.SignInAsync(user, isPersistent: false);

            TempData["NewPassword"] = newPassword;
            TempData["Success"] = "Your password has been reset successfully! Please change it after login.";
            
            return RedirectToAction("PasswordResetSuccess");
        }

        [HttpGet]
        public IActionResult PasswordResetSuccess()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResetPassword(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Users");
            }

            // Generate a new basic password
            var newPassword = "Pass@123";
            
            // Remove the old password
            var removePasswordResult = await _userManager.RemovePasswordAsync(user);
            if (!removePasswordResult.Succeeded)
            {
                TempData["Error"] = "Failed to reset password.";
                return RedirectToAction("Users");
            }

            // Add the new password
            var addPasswordResult = await _userManager.AddPasswordAsync(user, newPassword);
            if (!addPasswordResult.Succeeded)
            {
                TempData["Error"] = "Failed to reset password.";
                return RedirectToAction("Users");
            }

            TempData["Success"] = $"Password reset successfully! New password: {newPassword}";
            return RedirectToAction("Users");
        }
    }

    // View Models
    public class LoginViewModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public bool RememberMe { get; set; }
    }
 

    
}