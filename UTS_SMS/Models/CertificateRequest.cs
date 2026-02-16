using System.ComponentModel.DataAnnotations;

namespace SMS.Models
{
    public class CertificateRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int CertificateTypeId { get; set; }

        [Required]
        [Display(Name = "Issue Date")]
        public DateTime IssueDate { get; set; } = DateTime.Now;

        [Display(Name = "Is Paid")]
        public bool IsPaid { get; set; } = false;

        [Display(Name = "Generated Fine ID")]
        public int? GeneratedFineId { get; set; }

        [Required]
        public int CampusId { get; set; }

        [Required]
        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual Student Student { get; set; }
        public virtual CertificateType CertificateType { get; set; }
        public virtual StudentFineCharge? GeneratedFine { get; set; }
        public virtual Campus Campus { get; set; }
    }
}
