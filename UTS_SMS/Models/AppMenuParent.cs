using System.ComponentModel.DataAnnotations;

namespace UTS_SMS.Models
{
    public class AppMenuParent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string ControllerName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? IconClass { get; set; }

        public bool IsClickable { get; set; } = false;

        public bool HasChildren { get; set; } = false;

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        public string? AllowedRoles { get; set; } // Comma-separated role names

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string? CreatedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public string? ModifiedBy { get; set; }

        // Navigation property
        public virtual ICollection<AppMenuChild> Children { get; set; } = new List<AppMenuChild>();
    }
}
