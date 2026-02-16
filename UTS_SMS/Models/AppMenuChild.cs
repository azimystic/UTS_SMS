using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTS_SMS.Models
{
    public class AppMenuChild
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ParentMenuId { get; set; }

        [Required]
        [StringLength(100)]
        public string ActionName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? IconClass { get; set; }

        [StringLength(200)]
        public string? Url { get; set; }

        public bool IsIncluded { get; set; } = true;

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        public string? AllowedRoles { get; set; } // Comma-separated role names

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string? CreatedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public string? ModifiedBy { get; set; }

        // Navigation property
        [ForeignKey("ParentMenuId")]
        public virtual AppMenuParent? ParentMenu { get; set; }
    }
}
