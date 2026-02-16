using System.ComponentModel.DataAnnotations;

namespace UTS_SMS.Models
{
    public class BillingMaster
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int ClassId { get; set; }
        
        [Required]
        public int ForMonth { get; set; }   // 1 = January, etc.

        [Required]
        public int ForYear { get; set; }

        [Required]
        public int AcademicYear { get; set; }

        // Navigation property
        public virtual Student Student { get; set; }
        public virtual Class ClassObj { get; set; }
        public virtual ICollection<BillingTransaction> Transactions { get; set; } = new List<BillingTransaction>();

        // Calculated properties (could also be computed in queries)
        public decimal TotalPayable => TuitionFee + AdmissionFee + Fine + PreviousDues + MiscallaneousCharges;
        public decimal TotalPaid => Transactions.Sum(t => t.AmountPaid);
        public decimal Dues { get; set; }

        // Fee components
        public decimal TuitionFee { get; set; }
        public decimal MiscallaneousCharges { get; set; }
        public decimal AdmissionFee { get; set; } = 0;
        public decimal Fine { get; set; } = 0;
        public decimal PreviousDues { get; set; } = 0;
        public string? RemarksPreviousDues { get; set; }
        public string? Remarks  { get; set; }

        public decimal TotalAmountPayable { get; set; }
        public decimal TotalAmountPaid { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime ModifiedDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; }
        public int CampusId { get; set; }
        public Campus Campus { get; set; }
    }
}