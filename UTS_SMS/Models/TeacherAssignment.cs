using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public class TeacherAssignment
    {
        public int Id { get; set; }

        [Required]
        public int TeacherId { get; set; } // This would typically reference your User/Teacher table
        public Employee Teacher { get; set; }
        public int ClassId { get; set; }
        public Class Class { get; set; }

        public int SectionId { get; set; }
        public ClassSection Section { get; set; }

        public int SubjectId { get; set; }
        public Subject Subject { get; set; }

     
        public int CampusId { get; set; }
        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }
        public bool IsActive { get; set; } = true;

        // Unique constraint to ensure one teacher per subject-class-section combination
        // This would be enforced via database unique index or in business logic
    }
}
