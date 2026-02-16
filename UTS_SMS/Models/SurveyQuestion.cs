using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public class SurveyQuestion
    {
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string QuestionText { get; set; }

        [Required]
        public int QuestionOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }

        public int CampusId { get; set; }
        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }

        // Navigation property
        public virtual ICollection<StudentSurveyResponse> StudentResponses { get; set; } = new List<StudentSurveyResponse>();
    }

    public class StudentSurveyResponse
    {
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public Student Student { get; set; }

        [Required]
        public int SurveyQuestionId { get; set; }
        [ForeignKey("SurveyQuestionId")]
        public SurveyQuestion SurveyQuestion { get; set; }

        [Required]
        public bool Response { get; set; } // True for Yes, False for No

        public DateTime ResponseDate { get; set; } = DateTime.Now;

        public int CampusId { get; set; }
        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }

        // Add teacher reference for teacher-specific feedback
        public int? TeacherId { get; set; }
        [ForeignKey("TeacherId")]
        public Employee? Teacher { get; set; }
    }
}