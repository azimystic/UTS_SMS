// EmployeeAttendance.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTS_SMS.Models
{
    public class EmployeeAttendance
    {
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

         [DataType(DataType.Time)]
        public DateTime? TimeIn { get; set; }

        [DataType(DataType.Time)]
        public DateTime? TimeOut { get; set; }

        [Required]
        [StringLength(2)]
        [RegularExpression("^[PASL]$", ErrorMessage = "Status must be P (Present), A (Absent), S (Short Leave), or L (Leave)")]
        public string Status { get; set; } = "A"; // P=Present, A=Absent, S=Short Leave, L=Leave,T=Late

        public string? Remarks { get; set; }

        // For location-based attendance
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsLocationValid { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; } 

        // Navigation property
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }
    }

    public class EmployeeAttendanceViewModel
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }
        public bool HasAttendanceRecord { get; set; }
        public DateTime? TimeIn { get; set; }
        public DateTime? TimeOut { get; set; }
    }

    public class EmployeeDailySummaryViewModel
    {
        public string Role { get; set; }
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Leave { get; set; }
        public int Late { get; set; }
        public int ShortLeave { get; set; }
        public int TotalEmployees { get; set; }

        public int AttendancePercentage => TotalEmployees > 0
            ? (int)Math.Round((double)(Present + Late + ShortLeave) / TotalEmployees * 100)
            : 0;
    }
}