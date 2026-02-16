using System.ComponentModel.DataAnnotations;
using UTS_SMS.Models;

namespace SMS.ViewModels
{
    public class ProfileViewModel
    {
        public string Id { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string Username { get; set; }

        [Display(Name = "Current Password")]
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [Display(Name = "New Password")]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [Display(Name = "Confirm New Password")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string? ConfirmNewPassword { get; set; }

        public string? Role { get; set; }
        public bool IsAdmin { get; set; }

        // Profile Information
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? AvatarUrl { get; set; }

        // Additional fields for students/employees if needed
        public int? StudentId { get; set; }
        public int? EmployeeId { get; set; }

        // Additional properties for detailed views
        public Student? StudentDetails { get; set; }
        public Employee? EmployeeDetails { get; set; }

        // Document paths
        public List<string>? Documents { get; set; }
    }
}