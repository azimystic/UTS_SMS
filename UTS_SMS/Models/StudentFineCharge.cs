using System.ComponentModel.DataAnnotations;

namespace SMS.Models
{
    public class StudentFineCharge
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        [StringLength(200)]
        public string ChargeName { get; set; }

        [Required]
        public decimal Amount { get; set; }

        public string? Description { get; set; }

        [Required]
        public DateTime ChargeDate { get; set; } = DateTime.Now;

        public bool IsPaid { get; set; } = false;

        public DateTime? PaidDate { get; set; }

        public int? BillingMasterId { get; set; }

        [Required]
        public int CampusId { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual Student Student { get; set; }
        public virtual Campus Campus { get; set; }
        public virtual BillingMaster? BillingMaster { get; set; }
    }

    public class FineChargeTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Required]
        public decimal Amount { get; set; }

        public string? Description { get; set; }

        [Required]
        public int CampusId { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual Campus Campus { get; set; }
    }
}
