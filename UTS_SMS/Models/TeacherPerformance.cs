using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public class TeacherPerformance
    {
        public int Id { get; set; }

        [Required]
        public int TeacherId { get; set; }
        [ForeignKey("TeacherId")]
        public Employee Teacher { get; set; }

        [Required]
        public int Month { get; set; }

        [Required]
        public int Year { get; set; }

        // Attendance Score (3.5 marks max)
        [Column(TypeName = "decimal(3,2)")]
        public decimal AttendanceScore { get; set; }

        // Punctuality Score (2.5 marks max)
        [Column(TypeName = "decimal(3,2)")]
        public decimal PunctualityScore { get; set; }

        // Test Average Score (5.5 marks max)
        [Column(TypeName = "decimal(3,2)")]
        public decimal TestAverageScore { get; set; }

        // Survey Score (6 marks max)
        [Column(TypeName = "decimal(3,2)")]
        public decimal SurveyScore { get; set; }

        // Test Return Score (1.5 marks max)
        [Column(TypeName = "decimal(3,2)")]
        public decimal TestReturnScore { get; set; }

        // Checking Quality Score (1 mark max)
        [Column(TypeName = "decimal(3,2)")]
        public decimal CheckingQualityScore { get; set; }

        // Total Score (20 marks max)
        [Column(TypeName = "decimal(4,2)")]
        public decimal TotalScore { get; set; }

        // Performance details for transparency
        public int TotalWorkingDays { get; set; }
        public int AttendedDays { get; set; }
        public int OnTimeDays { get; set; }
        public decimal AverageTestMarks { get; set; }
        public int TotalSurveyResponses { get; set; }
        public int PositiveSurveyResponses { get; set; }
        public int TestsReturnedOnTime { get; set; }
        public int TotalTestsToReturn { get; set; }
        public int GoodCheckingCount { get; set; }
        public int BetterCheckingCount { get; set; }
        public int BadCheckingCount { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }

        public bool IsActive { get; set; } = true;

        public int CampusId { get; set; }
        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }

        // Calculate total score
        public void CalculateTotalScore()
        {
            TotalScore = AttendanceScore + PunctualityScore + TestAverageScore + 
                        SurveyScore + TestReturnScore + CheckingQualityScore;
        }
    }
}