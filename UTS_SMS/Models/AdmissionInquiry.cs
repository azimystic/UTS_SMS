using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTS_SMS.Models
{
    public class AdmissionInquiry
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string StudentName { get; set; }

        [Required]
        [StringLength(100)]
        public string FatherName { get; set; }

        [Required]
        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [StringLength(200)]
        public string? Email { get; set; }

        [Required]
        public int ClassInterestedId { get; set; }

        [ForeignKey("ClassInterestedId")]
        public Class ClassInterested { get; set; }

        [Required]
        public int CampusId { get; set; }

        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }

        [StringLength(500)]
        public string? PreviousSchool { get; set; }

        [Required]
        [StringLength(50)]
        public string InquiryStatus { get; set; } =     "New"; // New, Contacted, Visited, Enrolled, Rejected

        [DataType(DataType.Date)]
        public DateTime? VisitDate { get; set; }

        [StringLength(1000)]
        public string? Remarks { get; set; }

        [StringLength(200)]
        public string? Source { get; set; } // Website, Referral, Walk-in, Advertisement, etc.

        public DateTime InquiryDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? FollowUpDate { get; set; }

        public bool FollowUpRequired { get; set; } = false;

        public bool IsActive { get; set; } = true;
    }
}