using UTS_SMS.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
public class Student
{
    public int Id { get; set; }

    [Required]
    public string StudentName { get; set; }

    [Required]
     public string FatherName { get; set; }
   
    [Required]
    public string StudentCNIC { get; set; }
   
    [Required]
    public string FatherCNIC { get; set; }
    
    // Email field for user account creation
    [EmailAddress]
    public string? Email { get; set; }
    
    // ✅ ADD MOTHER FIELDS
    public string? MotherName { get; set; }
    public string? MotherCNIC { get; set; }
    public string? MotherPhone { get; set; }
    
    [Required]
    public string Gender { get; set; }

    [Required]
    public int Class { get; set; }
    public Class ClassObj { get; set; }
    [Required]
    public int Section { get; set; }
    public ClassSection SectionObj { get; set; }
    public int CampusId { get; set; }
    [ForeignKey("CampusId")]
    public Campus Campus { get; set; }
    public int SubjectsGroupingId { get; set; }
    
   
    
    public int? FamilyId { get; set; }
    [ForeignKey("FamilyId")]
    public Family? Family { get; set; }
    public SubjectsGrouping SubjectsGrouping { get; set; }

    public string? PhoneNumber { get; set; }

    [Required]
    public string FatherPhone { get; set; }

    [Required]
    public string HomeAddress { get; set; }

    public bool IsFatherDeceased { get; set; }
    public string? GuardianName { get; set; }
    public string? GuardianPhone { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }

    public string? FatherSourceOfIncome { get; set; }
    public string? PreviousSchool { get; set; }
    public decimal? TuitionFeeDiscountPercent { get; set; }
    public decimal? AdmissionFeeDiscountAmount { get; set; }
    
    // ✅ ADD STUDENT CATEGORY
    public int? StudentCategoryId { get; set; }
    [ForeignKey("StudentCategoryId")]
    public StudentCategory? StudentCategory { get; set; }
    
    public string? MatricRollNumber { get; set; }
    public string? InterRollNumber { get; set; }
    
    [StringLength(50)]
    public string? RollNumber { get; set; }  // Dynamic roll number: [CampusCode]-[YearOfAdmission]-[IncrementalNumber]
    
    public string? PersonalTitle { get; set; }
    public string? Notification { get; set; }
    
    // Image properties
    public string? ProfilePicture { get; set; }
    public string? FatherCNIC_Front { get; set; }
    public string? FatherCNIC_Back { get; set; }
    public string? BForm { get; set; }
    public string? StudentCNIC_Front { get; set; }
    public string? StudentCNIC_Back { get; set; }
    public string? MatricCertificate_01 { get; set; }
    public string? InterCertificate_01 { get; set; }
    public string? MatricCertificate_02 { get; set; }
    public string? InterCertificate_02 { get; set; }

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
 
    public bool AdmissionFeePaid { get; set; }
    public bool HasLeft { get; set; } = false;
    public DateTime? LeftDate { get; set; }
    public DateTime RegistrationDate { get; set; } = DateTime.Now;
    public string? RegisteredBy { get; set; }
}