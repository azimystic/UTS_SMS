using System.ComponentModel.DataAnnotations;

namespace UTS_SMS.Models
{
    public class Campus
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(20)]
        public string Code { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }

        [StringLength(500)]
        public string Address { get; set; }
        [StringLength(500)]
        public string Longitudes { get; set; }
        [StringLength(500)]
        public string Latitudes { get; set; }

        [StringLength(20)]
        public string Phone { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }
        public string? Logo { get; set; }
        
        public bool IsActive { get; set; } = true;

        // Navigation property
        public ICollection<Class> Classes { get; set; }
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
