using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTS_SMS.Models
{
    public class ExamDateSheet
    {
        public int Id { get; set; }

        [Required]
        public DateTime ExamDate { get; set; }

        [Required]
        public int ExamCategoryId { get; set; }

        [ForeignKey("ExamCategoryId")]
        public ExamCategory? ExamCategory { get; set; }

        [Required]
        public int ExamId { get; set; }

        [ForeignKey("ExamId")]
        public Exam? Exam { get; set; }

        [Required]
        public int SubjectId { get; set; }

        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; }

        [Required]
        public int CampusId { get; set; }

        [ForeignKey("CampusId")]
        public Campus? Campus { get; set; }

        [Required]
        [Range(1, 1000, ErrorMessage = "Total marks must be between 1 and 1000")]
        public decimal TotalMarks { get; set; } = 100;

        [Required]
        [Range(1, 1000, ErrorMessage = "Passing marks must be between 1 and 1000")]
        public decimal PassingMarks { get; set; } = 40;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string? CreatedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public string? ModifiedBy { get; set; }

        public bool IsActive { get; set; } = true;

        // Academic Year captured when exam is created (from Class's CurrentAcademicYear)
        public int? AcademicYear { get; set; }

        // Navigation property for class-section mappings
        public ICollection<ExamDateSheetClassSection> ClassSections { get; set; } = new List<ExamDateSheetClassSection>();
    }

    public class ExamDateSheetClassSection
    {
        public int Id { get; set; }

        [Required]
        public int ExamDateSheetId { get; set; }

        [ForeignKey("ExamDateSheetId")]
        public ExamDateSheet? ExamDateSheet { get; set; }

        [Required]
        public int ClassId { get; set; }

        [ForeignKey("ClassId")]
        public Class? Class { get; set; }

        [Required]
        public int SectionId { get; set; }

        [ForeignKey("SectionId")]
        public ClassSection? Section { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // ViewModel for creating/editing exam date sheet
    public class ExamDateSheetViewModel
    {
        public int Id { get; set; }
        public DateTime ExamDate { get; set; } = DateTime.Now;
        public int? ExamCategoryId { get; set; }
        public int? ExamId { get; set; }
        public int? SubjectId { get; set; }
        public int? CampusId { get; set; }
        public decimal TotalMarks { get; set; } = 100;
        public decimal PassingMarks { get; set; } = 40;
        public int? AcademicYear { get; set; }
        public bool IsUserCampusLocked { get; set; }
        
        // Class-Section rows to add
        public List<ClassSectionRow> ClassSectionRows { get; set; } = new List<ClassSectionRow>();

        // Collections for dropdowns
        public List<ExamCategory> ExamCategories { get; set; } = new List<ExamCategory>();
        public List<Exam> Exams { get; set; } = new List<Exam>();
        public List<Subject> Subjects { get; set; } = new List<Subject>();
        public List<Campus> Campuses { get; set; } = new List<Campus>();
        public List<Class> Classes { get; set; } = new List<Class>();
        public List<ClassSection> Sections { get; set; } = new List<ClassSection>();
    }

    public class ClassSectionRow
    {
        public int ClassId { get; set; }
        public int SectionId { get; set; }
    }

    // ViewModel for calendar display
    public class ExamDateSheetCalendarViewModel
    {
        public int? SelectedCampusId { get; set; }
        public DateTime SelectedMonth { get; set; } = DateTime.Now;
        public bool IsUserCampusLocked { get; set; }
        public List<ExamDateSheet> ExamDateSheets { get; set; } = new List<ExamDateSheet>();
        public List<Campus> Campuses { get; set; } = new List<Campus>();
        
        // Group exam date sheets by date for calendar display
        public Dictionary<DateTime, List<ExamDateSheet>> ExamsByDate 
        {
            get 
            {
                return ExamDateSheets
                    .GroupBy(e => e.ExamDate.Date)
                    .ToDictionary(g => g.Key, g => g.ToList());
            }
        }
    }
}
