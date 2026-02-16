using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public class Attendance
    {
        public int Id { get; set; }
        public int CampusId { get; set; }
        public Campus Campus { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        public int AcademicYear { get; set; }

        [Required]
        [StringLength(1)]
        [RegularExpression("^[PAL]$", ErrorMessage = "Status must be P (Present), A (Absent), or L (Leave)")]
        public string Status { get; set; } = "A"; // P=Present, A=Absent, L=Leave

        public string? Remarks { get; set; }

        // Changed from string to int for foreign key relationships
        [Required]
        public int ClassId { get; set; }

        [Required]
        public int SectionId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("StudentId")]
        public virtual Student Student { get; set; }

        [ForeignKey("ClassId")]
        public virtual Class ClassObj { get; set; }

        [ForeignKey("SectionId")]
        public virtual ClassSection SectionObj { get; set; }
    }

    public class AttendanceViewModel
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string Status { get; set; }
        public bool HasAttendanceRecord { get; set; }
    }
    
}