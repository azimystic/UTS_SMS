using System.ComponentModel.DataAnnotations;

namespace SMS.Models
{
    public class CertificateType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Certificate Name")]
        public string CertificateName { get; set; }

        [Required]
        [Display(Name = "Price")]
        public decimal Price { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Report File Name")]
        public string ReportFileName { get; set; }

        [Required]
        public int CampusId { get; set; }

        [Required]
        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual Campus Campus { get; set; }
        public virtual ICollection<CertificateRequest> CertificateRequests { get; set; }
    }
}
