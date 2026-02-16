using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public class Chapter
    {
        public int Id { get; set; }

        [Required]
        public int SubjectId { get; set; }
        
        [ForeignKey("SubjectId")]
        public Subject Subject { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Required]
        public int ChapterNumber { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public string CreatedBy { get; set; }

        public DateTime? ModifiedAt { get; set; }

        public string? ModifiedBy { get; set; }

        public int CampusId { get; set; }
        
        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<ChapterSection> ChapterSections { get; set; } = new List<ChapterSection>();
        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<ChapterMaterial> ChapterMaterials { get; set; } = new List<ChapterMaterial>();
    }
}
