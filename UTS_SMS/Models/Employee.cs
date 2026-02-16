using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace SMS.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; }
        [MaxLength(20)]
        [Required]
        public string CNIC { get; set; }

        // Email field for user account creation
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string Gender { get; set; } // Male, Female, Other

        [Required]
        public string Role { get; set; } // Teacher, Accountant, Admin, Aya, Guard, Lab Instructor

        // Education fields
        public string? EducationLevel { get; set; } // Matric, FSC, BS, BSc, MSc, PhD, etc.
        public string? Degree { get; set; }
        public string? MajorSubject { get; set; }
        public decimal? MatricMarks { get; set; }
        public decimal? InterMarks { get; set; }
        public decimal? CGPA { get; set; }
        public string? University { get; set; }

        // Document properties
        public string? ProfilePicture { get; set; }
        public string? CNIC_Front { get; set; }
        public string? CNIC_Back { get; set; }
        public string? MatricCertificate { get; set; }
        public string? InterCertificate { get; set; }
        public string? DegreeCertificate { get; set; }
        public string? OtherQualifications { get; set; }

        public DateTime JoiningDate { get; set; } = DateTime.Now;
        public DateTime? LeavingDate { get; set; }
        public   TimeOnly? OnTime { get; set; }
        public    TimeOnly? OffTime { get; set; }
        public int LateTimeFlexibility { get; set; }
         public bool IsActive { get; set; } = true;
        public string? RegisteredBy { get; set; }
        public int CampusId { get; set; }
        public Campus Campus { get; set; }

        // Helper method for file upload
        public async Task<string> UploadFile(IFormFile file, string folderName, IWebHostEnvironment env)
        {
            if (file == null || file.Length == 0)
                return null;

            var uploadsFolder = Path.Combine(env.WebRootPath, "uploads", folderName);
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return Path.Combine("uploads", folderName, uniqueFileName).Replace("\\", "/");
        }
    }
}