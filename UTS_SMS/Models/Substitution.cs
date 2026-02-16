using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public class Substitution
    {
        public int Id { get; set; }

        [Required]
        public int TimetableSlotId { get; set; }
        [ForeignKey("TimetableSlotId")]
        public TimetableSlot? TimetableSlot { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        public int OriginalTeacherId { get; set; }
        [ForeignKey("OriginalTeacherId")]
        public Employee? OriginalTeacher { get; set; }

        [Required]
        public int SubstituteEmployeeId { get; set; }
        [ForeignKey("SubstituteEmployeeId")]
        public Employee? SubstituteEmployee { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        public int CampusId { get; set; }
        [ForeignKey("CampusId")]
        public Campus? Campus { get; set; }

        [StringLength(100)]
        public string? CreatedBy { get; set; }
    }
}
