using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public class NamazAttendance
    {
        public int Id { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.Today;

        // Can be either StudentId or EmployeeId
        public int? StudentId { get; set; }
        public int? EmployeeId { get; set; }

        [Required]
        public int CampusId { get; set; }

        [Required]
        public int AcademicYear { get; set; }

        [Required]
        [StringLength(3)]
        [RegularExpression("^(WJ|QZ|WOJ)$", ErrorMessage = "Status must be WJ (With Jamat), QZ (Qaza), or WOJ (Without Jamat)")]
        public string Status { get; set; } = "WJ"; // WJ=With Jamat, QZ=Qaza, WOJ=Without Jamat

        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        [ForeignKey("CampusId")]
        public virtual Campus Campus { get; set; }

        // Helper property to get the person's name
        public string PersonName => Student?.StudentName ?? Employee?.FullName ?? "Unknown";
        
        // Helper property to get the person type
        public string PersonType => StudentId.HasValue ? "Student" : "Employee";
    }

    public class NamazAttendanceViewModel
    {
        public int? StudentId { get; set; }
        public int? EmployeeId { get; set; }
        public string PersonName { get; set; }
        public string PersonType { get; set; }
        public string Status { get; set; }
        public bool HasAttendanceRecord { get; set; }
        public string? Remarks { get; set; }
    }

    public class NamazDailySummaryViewModel
    {
        public string PersonType { get; set; }
        public int WithJamat { get; set; }
        public int Qaza { get; set; }
        public int WithoutJamat { get; set; }
        public int TotalPersons { get; set; }

        public int AttendancePercentage => TotalPersons > 0
            ? (int)Math.Round((double)WithJamat / TotalPersons * 100)
            : 0;
    }
}