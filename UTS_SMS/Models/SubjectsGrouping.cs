using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public class SubjectsGrouping
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        public int CampusId { get; set; }
        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
         public ICollection<SubjectsGroupingDetails> SubjectsGroupingDetails { get; set; }
    }
    public class SubjectsGroupingDetails
    {
        public int Id { get; set; }
        [Required]
        public int SubjectId { get; set; }
        public Subject Subject { get; set; }
        [Required]
        public int SubjectsGroupingId { get; set; }
        public SubjectsGrouping SubjectsGrouping { get; set; }
        public bool IsActive { get; set; } = true;
       
    }
}
