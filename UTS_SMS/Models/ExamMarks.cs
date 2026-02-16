using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTS_SMS.Models
{
    public class ExamMarks
    {
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public Student Student { get; set; }

        [Required]
        public int ExamId { get; set; }

        [ForeignKey("ExamId")]
        public Exam Exam { get; set; }
        [Required]
        public int AcademicYear { get; set; }

        [Required]
        public int SubjectId { get; set; }

        [ForeignKey("SubjectId")]
        public Subject Subject { get; set; }

        [Required]
        public int ClassId { get; set; }

        [ForeignKey("ClassId")]
        public Class Class { get; set; }

        [Required]
        public int SectionId { get; set; }

        [ForeignKey("SectionId")]
        public ClassSection Section { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Total marks must be a positive number")]
        public decimal TotalMarks { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Passing marks must be a positive number")]
        public decimal PassingMarks { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Obtained marks cannot be negative")]
        public decimal ObtainedMarks { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } // Pass, Fail, 1st Division, 2nd Division, 3rd Division

        [StringLength(10)]
        public string Grade { get; set; } // A+, A, B+, B, C, D, F

        public decimal Percentage { get; set; }

        public DateTime ExamDate { get; set; }

        public string? Remarks { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string? CreatedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public string? ModifiedBy { get; set; }

        public bool IsActive { get; set; } = true;
        public int CampusId { get; set; }
        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }

        // Helper method to calculate status based on marks
        public void CalculateStatusAndGrade()
        {
            Percentage = TotalMarks > 0 ? Math.Round((ObtainedMarks / TotalMarks) * 100, 2) : 0;

            if (ObtainedMarks < PassingMarks)
            {
                Status = "Fail";
                Grade = "F";
            }
            else if (Percentage >= 80)
            {
                Status = "1st Division";
                Grade = Percentage >= 90 ? "A+" : "A";
            }
            else if (Percentage >= 70)
            {
                Status = "2nd Division";
                Grade = Percentage >= 75 ? "B+" : "B";
            }
            else if (Percentage >= 50)
            {
                Status = "3rd Division";
                Grade = "C";
            }
            else
            {
                Status = "Pass";
                Grade = "D";
            }
        }
    }

    // ViewModel for the exam marks entry process
    public class ExamMarksEntryViewModel
    {
        public int? SelectedExamCategoryId { get; set; }
        public int? SelectedExamId { get; set; }
        public int? SelectedClassId { get; set; }
        public int? SelectedSectionId { get; set; }
        public int? SelectedSubjectId { get; set; }
        public int? SelectedAcademicYear { get; set; }
        public decimal TotalMarks { get; set; }
        public decimal PassingMarks { get; set; }
        public DateTime ExamDate { get; set; } = DateTime.Now;

        // Collections for dropdowns
        public List<ExamCategory> ExamCategories { get; set; } = new List<ExamCategory>();
        public List<Exam> Exams { get; set; } = new List<Exam>();
        public List<Class> Classes { get; set; } = new List<Class>();
        public List<Campus> Campuses { get; set; } = new List<Campus>();
        public List<ClassSection> Sections { get; set; } = new List<ClassSection>();
        public List<Subject> Subjects { get; set; } = new List<Subject>();
        public List<AcademicYear> AcademicYears { get; set; } = new List<AcademicYear>();

        // Student marks for entry
        public List<StudentMarksEntry> StudentMarks { get; set; } = new List<StudentMarksEntry>();
    }

    public class StudentMarksEntry
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string FatherName { get; set; }
        public bool IsEnrolledInSubject { get; set; }
        public decimal ObtainedMarks { get; set; }
        public string Status { get; set; }
        public string Grade { get; set; }
        public decimal Percentage { get; set; }
        public string? Remarks { get; set; }
        public bool HasExistingMarks { get; set; }
        public int? ExamMarksId { get; set; }
    }

    // ViewModel for viewing and analyzing exam marks
    public class ExamMarksAnalysisViewModel
    {
        public List<ExamMarks> ExamMarksList { get; set; } = new List<ExamMarks>();
        
        // Statistics - Focus on marks and pass/fail only
        public decimal AverageMarks { get; set; }
        public decimal AveragePercentage { get; set; }
        public decimal HighestMarks { get; set; }
        public decimal LowestMarks { get; set; }
        public int TotalStudents { get; set; }
        public int PassedStudents { get; set; }
        public int FailedStudents { get; set; }
        public decimal PassPercentage { get; set; }

        // Filter properties
        public int? ExamCategoryId { get; set; }
        public int? ExamId { get; set; }
        public int? ClassId { get; set; }
        public int? CampusId { get; set; }
        public int? SectionId { get; set; }
        public int? SubjectId { get; set; }
        public int? StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? FilterMode { get; set; } // "classwise" or "testwise" or "studentwise"
        public int? AcademicYear { get; set; } // Filter by academic year
        
        // User role info
        public bool IsOwner { get; set; }

        // Collections for filters
        public List<ExamCategory> ExamCategories { get; set; } = new List<ExamCategory>();
        public List<Exam> Exams { get; set; } = new List<Exam>();
        public List<Class> Classes { get; set; } = new List<Class>();
        public List<Campus> Campuses { get; set; } = new List<Campus>();
        public List<ClassSection> Sections { get; set; } = new List<ClassSection>();
        public List<Subject> Subjects { get; set; } = new List<Subject>();
        public List<AcademicYear> AcademicYears { get; set; } = new List<AcademicYear>();
    }
}