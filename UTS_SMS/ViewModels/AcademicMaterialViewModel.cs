using UTS_SMS.Models;
using System.ComponentModel.DataAnnotations;

namespace UTS_SMS.ViewModels
{
    public class AcademicMaterialViewModel
    {
        // Chapter Information
        public int? ChapterId { get; set; }
        
        [Required]
        public int SubjectId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string ChapterName { get; set; }
        
        [Required]
        public int ChapterNumber { get; set; }
        
        [StringLength(1000)]
        public string? ChapterDescription { get; set; }
        
        // Sections
        public List<ChapterSectionViewModel> Sections { get; set; } = new List<ChapterSectionViewModel>();
        
        // Questions
        public List<QuestionViewModel> Questions { get; set; } = new List<QuestionViewModel>();
        
        // Materials
        public List<ChapterMaterialViewModel> Materials { get; set; } = new List<ChapterMaterialViewModel>();
        
        // Existing Materials (for edit mode)
        public List<ChapterMaterial> ExistingMaterials { get; set; } = new List<ChapterMaterial>();
        
        // For display purposes
        public Subject? Subject { get; set; }
    }
    
    public class ChapterSectionViewModel
    {
        public int? Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; }
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        public int DisplayOrder { get; set; }
        
        public bool IsDeleted { get; set; } = false;
    }
    
    public class QuestionViewModel
    {
        public int? Id { get; set; }
        
        public int? SectionId { get; set; }
        
        [Required]
        public QuestionType Type { get; set; }
        
        [Required]
        [StringLength(2000)]
        public string QuestionText { get; set; }
        
        [StringLength(4000)]
        public string? Answer { get; set; }
        
        // For MCQ
        [StringLength(500)]
        public string? OptionA { get; set; }
        
        [StringLength(500)]
        public string? OptionB { get; set; }
        
        [StringLength(500)]
        public string? OptionC { get; set; }
        
        [StringLength(500)]
        public string? OptionD { get; set; }
        
        [StringLength(1)]
        public string? CorrectOption { get; set; }
        
        public int DisplayOrder { get; set; }
        
        public bool IsDeleted { get; set; } = false;
    }
    
    public class ChapterMaterialViewModel
    {
        public int? Id { get; set; }
        
        public int? SectionId { get; set; }
        
        [Required]
        public MaterialType Type { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Heading { get; set; }
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public IFormFile? File { get; set; }
        
        public int DisplayOrder { get; set; }
        
        public bool IsDeleted { get; set; } = false;
    }
    
    public class StudentAcademicMaterialViewModel
    {
        public Chapter Chapter { get; set; }
        public List<ChapterSection> Sections { get; set; }
        public List<Question> Questions { get; set; }
        public List<ChapterMaterial> Materials { get; set; }
    }
}
