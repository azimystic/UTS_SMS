using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public class TestReturn
    {
        public int Id { get; set; }

        [Required]
        public int ExamId { get; set; }
        [ForeignKey("ExamId")]
        public Exam Exam { get; set; }

        [Required]
        public int SubjectId { get; set; }
        [ForeignKey("SubjectId")]
        public Subject Subject { get; set; }

        [Required]
        public int ClassId { get; set; }
        [ForeignKey("ClassId")]
        public Class Class { get; set; }

        [Required]
        public int SectionId { get; set; }
        [ForeignKey("SectionId")]
        public ClassSection Section { get; set; }

        [Required]
        public int TeacherId { get; set; }
        [ForeignKey("TeacherId")]
        public Employee Teacher { get; set; }

        [Required]
        public DateTime ExamDate { get; set; }

        public DateTime? ReturnDate { get; set; }

        [Required]
        public bool IsReturnedOnTime { get; set; } = false;

        [Required]
        [StringLength(20)]
        public string CheckingQuality { get; set; } = "Good"; // Good, Better, Bad

        [StringLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }

        public bool IsActive { get; set; } = true;

        public int CampusId { get; set; }
        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }

        // Not mapped - used only for form binding
        [NotMapped]
        public int? ExamCategoryId { get; set; }
    }
}