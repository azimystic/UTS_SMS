using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public class ChapterSection
    {
        public int Id { get; set; }

        [Required]
        public int ChapterId { get; set; }
        
        [ForeignKey("ChapterId")]
        public Chapter Chapter { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        public int DisplayOrder { get; set; } = 1;

        [StringLength(1000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<ChapterMaterial> ChapterMaterials { get; set; } = new List<ChapterMaterial>();
    }
}
