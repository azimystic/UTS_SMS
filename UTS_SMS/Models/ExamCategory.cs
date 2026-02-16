using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public class ExamCategory
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        // CampusId = null means "All Campuses"
        public int? CampusId { get; set; }

        [ForeignKey("CampusId")]
        public Campus? Campus { get; set; }

        public bool IsActive { get; set; } = true;
        
        // Helper property to check if this category is for all campuses
        [NotMapped]
        public bool IsForAllCampuses => CampusId == null;
    }

    public class Exam
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        public int ExamCategoryId { get; set; }

        [ForeignKey("ExamCategoryId")]
        public ExamCategory ExamCategory { get; set; }

        // CampusId = null means "All Campuses"
        public int? CampusId { get; set; }

        [ForeignKey("CampusId")]
        public Campus? Campus { get; set; }

        public bool IsActive { get; set; } = true;
        
        // Helper property to check if this exam is for all campuses
        [NotMapped]
        public bool IsForAllCampuses => CampusId == null;
    }
}