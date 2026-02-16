using System.ComponentModel.DataAnnotations;

namespace SMS.Models
{
    // Class Section Model
    public class ClassSection
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

 

        public int ClassId { get; set; }
        public Class Class { get; set; }

        public int Capacity { get; set; }

        // Current Academic Year for this section (e.g., "2025-2026")
        [StringLength(20)]
        public string? CurrentAcademicYear { get; set; }

        public bool IsActive { get; set; } = true;
        public int CampusId { get; set; }
        public Campus Campus { get; set; }

        // Navigation property
        public ICollection<TeacherAssignment> TeacherAssignments { get; set; }
    }
}
