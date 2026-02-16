using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public enum QuestionType
    {
        ShortQuestion,
        LongQuestion,
        MCQ
    }

    public class Question
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
        public QuestionType Type { get; set; }

        [Required]
        [StringLength(2000)]
        public string QuestionText { get; set; }

        [StringLength(4000)]
        public string? Answer { get; set; }

        // For MCQ only
        [StringLength(500)]
        public string? OptionA { get; set; }

        [StringLength(500)]
        public string? OptionB { get; set; }

        [StringLength(500)]
        public string? OptionC { get; set; }

        [StringLength(500)]
        public string? OptionD { get; set; }

        [StringLength(1)]
        public string? CorrectOption { get; set; } // A, B, C, or D

        public int DisplayOrder { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;
    }
}
