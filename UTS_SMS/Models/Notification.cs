using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Type { get; set; } // complaint, fee, exam, general, attendance, etc.

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string Message { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }

        // Target user information
        [StringLength(450)]
        public string? UserId { get; set; } // If null, notification is for all admins

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [StringLength(100)]
        public string? TargetRole { get; set; } // Admin, Teacher, Student, Parent, etc.

        // Related entity information
        public int? RelatedEntityId { get; set; } // ID of related complaint, student, exam, etc.

        [StringLength(100)]
        public string? RelatedEntityType { get; set; } // StudentComplaint, BillingTransaction, Exam, etc.

        [StringLength(500)]
        public string? ActionUrl { get; set; } // URL to navigate when notification is clicked

        public int CampusId { get; set; }

        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;
    }
}
