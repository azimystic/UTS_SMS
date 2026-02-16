using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTS_SMS.Models
{
    public class StudentHistory
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
        public int ClassId { get; set; }

        [ForeignKey("ClassId")]
        public Class Class { get; set; }

        [Required]
        public int SectionId { get; set; }

        [ForeignKey("SectionId")]
        public ClassSection Section { get; set; }

        [Required]
        [StringLength(100)]
        public string Award { get; set; } // Elite 1st, Elite 2nd, Diamond 1st, etc.

        [Required]
        public int Position { get; set; } // 1-9

        [Required]
        [Range(0, 100)]
        public decimal FinalPercentage { get; set; }

        public DateTime ComputedDate { get; set; } = DateTime.Now;

        public string? ComputedBy { get; set; }

        public bool IsActive { get; set; } = true;

 
      }

    // DTO for position computation
    public class StudentPositionDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string FatherName { get; set; }
        public string ClassName { get; set; }
        public string SectionName { get; set; }
        public decimal FinalPercentage { get; set; }
        public int Position { get; set; }
        public string Award { get; set; }
        public string AwardIcon { get; set; }
        public string AwardColor { get; set; }
        public bool HasExistingRecord { get; set; }
        public int? ExistingHistoryId { get; set; }
    }

    // DTO for position computation request
    public class PositionComputationRequestDto
    {
        public int ExamCategoryId { get; set; }
        public int ExamId { get; set; }
        public int AcademicYear { get; set; }
        public int ClassId { get; set; }
    }

    // DTO for save positions request
    public class SavePositionsRequestDto
    {
        public int ExamId { get; set; }
        public int AcademicYear { get; set; }
        public int ClassId { get; set; }
        public List<StudentPositionDto> Positions { get; set; } = new List<StudentPositionDto>();
        public bool OverwriteExisting { get; set; } = false;
    }

    // Static class for award definitions
    public static class AwardTypes
    {
        public static readonly Dictionary<int, (string Award, string Icon, string Color)> Awards = new()
        {
            { 1, ("Elite 1st", "👑", "text-yellow-500") },
            { 2, ("Elite 2nd", "🥇", "text-yellow-400") },
            { 3, ("Elite 3rd", "🥈", "text-gray-400") },
            { 4, ("Diamond 1st", "💎", "text-blue-500") },
            { 5, ("Diamond 2nd", "💎", "text-blue-400") },
            { 6, ("Diamond 3rd", "💎", "text-blue-300") },
            { 7, ("Gold 1st", "🏆", "text-orange-500") },
            { 8, ("Gold 2nd", "🏆", "text-orange-400") },
            { 9, ("Gold 3rd", "🏆", "text-orange-300") }
        };

        public static (string Award, string Icon, string Color) GetAward(int position)
        {
            return Awards.ContainsKey(position) ? Awards[position] : ("", "", "");
        }
    }

    // DTO for position computation filters
    public class PositionFiltersDto
    {
        public List<ExamCategory> ExamCategories { get; set; } = new List<ExamCategory>();
        public List<Exam> Exams { get; set; } = new List<Exam>();
        public List<int> AvailableYears { get; set; } = new List<int>();
         public int? SelectedExamCategoryId { get; set; }
        public int? SelectedExamId { get; set; }
        public int? SelectedAcademicYear { get; set; }
     }

    // DTO for student performance summary
    public class StudentPerformanceSummaryDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string FatherName { get; set; }
        public string ClassName { get; set; }
        public string SectionName { get; set; }
        public int TotalSubjects { get; set; }
        public decimal TotalObtainedMarks { get; set; }
        public decimal TotalMaxMarks { get; set; }
        public decimal FinalPercentage { get; set; }
        public List<SubjectPerformanceDto> SubjectPerformances { get; set; } = new List<SubjectPerformanceDto>();
    }

    // DTO for individual subject performance
    public class SubjectPerformanceDto
    {
        public string SubjectName { get; set; }
        public decimal ObtainedMarks { get; set; }
        public decimal TotalMarks { get; set; }
        public decimal Percentage { get; set; }
        public string Grade { get; set; }
        public string Status { get; set; }
        public bool IsEnrolled { get; set; } = true;
        public bool IsCounted { get; set; } = true;
    }
}