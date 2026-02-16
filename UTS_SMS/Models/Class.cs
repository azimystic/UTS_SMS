using System.ComponentModel.DataAnnotations;

namespace UTS_SMS.Models
{
    public class Class
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        public int? ClassTeacherId { get; set; }
        public Employee? ClassTeacher { get; set; }   // ✅ Navigation to Employee
         

        
        public string GradeLevel { get; set; }

        // Current Academic Year for this class (e.g., "2025-2026")
        [StringLength(20)]
        public string? CurrentAcademicYear { get; set; }

        public int CampusId { get; set; }
        public Campus Campus { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<ClassSection> ClassSections { get; set; }
        public ICollection<TeacherAssignment> TeacherAssignments { get; set; }
    }
}
