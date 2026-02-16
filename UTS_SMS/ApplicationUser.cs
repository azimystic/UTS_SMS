using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTS_SMS.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Link to Student, Employee, or Family (for parents)
        public int? StudentId { get; set; }
        public int? EmployeeId { get; set; }
        public int? FamilyId { get; set; }

        public string? FullName { get; set; }
        public string? Address { get; set; }
        public string? AvatarUrl { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
        public int? CampusId { get; set; }
        
        [ForeignKey("CampusId")]
        public Campus? Campus { get; set; }

        // Navigation properties
        public virtual Student? Student { get; set; }
        public virtual Employee? Employee { get; set; }
        public virtual Family? Family { get; set; }
    }
}