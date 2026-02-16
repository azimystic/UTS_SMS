using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public enum MaterialType
    {
        Notes,
        Book,
        Image,
        PDF
    }

    public class ChapterMaterial
    {
        public int Id { get; set; }

        [Required]
        public int ChapterId { get; set; }
        
        [ForeignKey("ChapterId")]
        public Chapter Chapter { get; set; }

        public int? ChapterSectionId { get; set; }
        
        [ForeignKey("ChapterSectionId")]
        public ChapterSection? ChapterSection { get; set; }

        [Required]
        public MaterialType Type { get; set; }

        [Required]
        [StringLength(200)]
        public string Heading { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; }

        [StringLength(200)]
        public string? OriginalFileName { get; set; }

        public int DisplayOrder { get; set; } = 1;

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public string? UploadedBy { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
