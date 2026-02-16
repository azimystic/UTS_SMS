using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTS_SMS.Models
{
    public class DiaryImage
    {
        public int Id { get; set; }

        [Required]
        public int DiaryId { get; set; }

        [Required]
        [StringLength(500)]
        public string ImagePath { get; set; }

        [StringLength(200)]
        public string? OriginalFileName { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; } = 1;

        public DateTime UploadedAt { get; set; } = DateTime.Now;
        public string? UploadedBy { get; set; }

        // Navigation property
        [ForeignKey("DiaryId")]
        public virtual Diary Diary { get; set; }
    }
}