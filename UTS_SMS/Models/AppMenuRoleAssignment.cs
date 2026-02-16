using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    /// <summary>
    /// Maps roles to menu items (both parent and child menus)
    /// </summary>
    public class AppMenuRoleAssignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string RoleName { get; set; } = string.Empty;

        // One of these will be set, not both
        public int? ParentMenuId { get; set; }
        public int? ChildMenuId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }

        // Navigation properties
        [ForeignKey("ParentMenuId")]
        public virtual AppMenuParent? ParentMenu { get; set; }

        [ForeignKey("ChildMenuId")]
        public virtual AppMenuChild? ChildMenu { get; set; }
    }
}
