using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public class AdminNotification
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Action { get; set; }  // e.g., "Student Created", "Student Edited", "Pickup Card Added", etc.

        [Required]
        [StringLength(500)]
        public string Description { get; set; }  // Detailed description of the action

        [Required]
        public DateTime ActionDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string PerformedBy { get; set; }  // Username or identifier of user who performed the action

        // Optional reference to related entities
        public int? StudentId { get; set; }
        [ForeignKey("StudentId")]
        public Student? Student { get; set; }

        public int? PickupCardId { get; set; }
        [ForeignKey("PickupCardId")]
        public PickupCard? PickupCard { get; set; }

        public int? CampusId { get; set; }
        [ForeignKey("CampusId")]
        public Campus? Campus { get; set; }

        [StringLength(50)]
        public string? EntityType { get; set; }  // "Student", "PickupCard", etc.

        public int? EntityId { get; set; }  // Generic reference to the entity

        public bool IsRead { get; set; } = false;  // Track if notification has been read
    }
}
