using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public class ClassFeeExtraChargePaymentHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public Student Student { get; set; }

        [Required]
        public int ClassFeeExtraChargeId { get; set; }
        [ForeignKey("ClassFeeExtraChargeId")]
        public ClassFeeExtraCharges ClassFeeExtraCharge { get; set; }

        // For OncePerClass category, track which class it was paid for
        public int? ClassIdPaidFor { get; set; }
        [ForeignKey("ClassIdPaidFor")]
        public Class? ClassPaidFor { get; set; }

        public DateTime PaymentDate { get; set; }
        
        [Required]
        public int BillingMasterId { get; set; }
        [ForeignKey("BillingMasterId")]
        public BillingMaster BillingMaster { get; set; }

        public decimal AmountPaid { get; set; }

        public int CampusId { get; set; }
        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }
    }
}
