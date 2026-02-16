using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTS_SMS.Models
{
    public class StudentMigration
    {
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public Student Student { get; set; }

        [Required]
        public int FromCampusId { get; set; }

        [ForeignKey("FromCampusId")]
        public Campus FromCampus { get; set; }

        [Required]
        public int ToCampusId { get; set; }

        [ForeignKey("ToCampusId")]
        public Campus ToCampus { get; set; }

        public int FromClassId { get; set; }
        public int FromSectionId { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public decimal OutstandingDues { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        [Required]
        public DateTime RequestedDate { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(100)]
        public string RequestedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }

        [MaxLength(100)]
        public string? ApprovedBy { get; set; }

        public DateTime? ProcessedDate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
