using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTS_SMS.Models
{
    public class ToDo
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public bool IsCompleted { get; set; } = false;

        [Required]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; } = DateTime.Today;

        [Required]
        [StringLength(20)]
        public string Priority { get; set; } = "Medium"; // Low, Medium, High

        public int? CampusId { get; set; }

        [ForeignKey("CampusId")]
        public Campus? Campus { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
