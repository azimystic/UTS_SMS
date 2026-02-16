// Models/Diary.cs
using UTS_SMS.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Diary
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TeacherAssignmentId { get; set; }

    [ForeignKey("TeacherAssignmentId")]
    public virtual TeacherAssignment TeacherAssignment { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    [StringLength(500, ErrorMessage = "Lesson summary cannot exceed 500 characters.")]
    public string LessonSummary { get; set; }

    [StringLength(500)]
    public string HomeworkGiven { get; set; }

    [StringLength(500)]
    public string Notes { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Required]
    public string CreatedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public string ModifiedBy { get; set; }
    public int CampusId { get; set; }
    public Campus Campus { get; set; }
    
    // Optional link to chapter
    public int? ChapterId { get; set; }
    public Chapter? Chapter { get; set; }
    
    // Optional link to chapter section
    public int? ChapterSectionId { get; set; }
    public ChapterSection? ChapterSection { get; set; }
    
    // Navigation property for diary images
    public virtual ICollection<DiaryImage> DiaryImages { get; set; } = new List<DiaryImage>();
}