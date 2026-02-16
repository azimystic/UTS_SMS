using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public class StudentComplaint
    {
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public Student Student { get; set; }

        [Required]
        [StringLength(200)]
        public string ComplaintTitle { get; set; }

        [Required]
        [StringLength(2000)]
        public string ComplaintDescription { get; set; }

        [Required]
        [StringLength(50)]
        public string ComplaintType { get; set; } // Academic, Behavioral, Financial, Administrative, Facility, Other

        [Required]
        [StringLength(50)]
        public string Priority { get; set; } // High, Medium, Low

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Open"; // Open, In Investigation, Resolved, Closed, Escalated

        [DataType(DataType.Date)]
        public DateTime ComplaintDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? ReportedBy { get; set; } // Can be student, parent, or staff member

        [StringLength(50)]
        public string? ReporterType { get; set; } // Student, Parent, Staff

        [StringLength(20)]
        public string? ReporterPhone { get; set; }

        [StringLength(200)]
        public string? ReporterEmail { get; set; }

        [Required]
        public int CampusId { get; set; }

        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }

        [StringLength(100)]
        public string? AssignedTo { get; set; } // Staff member responsible for resolution

        public DateTime? AssignedDate { get; set; }

        [StringLength(2000)]
        public string? InvestigationNotes { get; set; }

        [StringLength(2000)]
        public string? ResolutionDetails { get; set; }

        [StringLength(2000)]
        public string? ResolutionComments { get; set; }

        public DateTime? ResolvedDate { get; set; }

        // Teacher-related fields
        public int? TeacherId { get; set; }

        [ForeignKey("TeacherId")]
        public Employee? Teacher { get; set; }

        [StringLength(2000)]
        public string? TeacherComments { get; set; }

        public DateTime? TeacherCommentDate { get; set; }

        [StringLength(100)]
        public string? ResolvedBy { get; set; }

        [StringLength(50)]
        public string? SatisfactionLevel { get; set; } // Very Satisfied, Satisfied, Neutral, Dissatisfied, Very Dissatisfied

        [StringLength(1000)]
        public string? FeedbackComments { get; set; }

        public bool IsAnonymous { get; set; } = false;

        public bool RequiresParentNotification { get; set; } = false;

        public bool ParentNotified { get; set; } = false;

        public DateTime? ParentNotificationDate { get; set; }

        [StringLength(1000)]
        public string? FollowUpActions { get; set; }

        public DateTime? NextFollowUpDate { get; set; }

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}