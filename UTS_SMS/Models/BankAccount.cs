using System.ComponentModel.DataAnnotations;

namespace SMS.Models
{
    public class BankAccount
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string BankName { get; set; }

        [Required]
        [StringLength(50)]
        public string AccountNumber { get; set; }

        [Required]
        [StringLength(100)]
        public string AccountTitle { get; set; }

        [StringLength(200)]
        public string Branch { get; set; }

        [StringLength(20)]
        public string BranchCode { get; set; }

        public bool IsActive { get; set; } = true;
        public int CampusId { get; set; }
        public Campus Campus { get; set; }
    }
}
