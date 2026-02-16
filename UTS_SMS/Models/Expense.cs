using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTS_SMS.Models
{
    public class Expense
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Amount must be a positive number")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime ExpenseDate { get; set; } = DateTime.Now;

        [Required]
        public int Month { get; set; }

        [Required]
        public int Year { get; set; }

        public int? AccountId { get; set; }
        [ForeignKey("AccountId")]
        public BankAccount? Account { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        [StringLength(100)]
        public string? Reference { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }

        public bool IsActive { get; set; } = true;

        public int CampusId { get; set; }
        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }
    }
}