using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTS_SMS.Models
{
    public class PickupCard
    {
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }
        
        [ForeignKey("StudentId")]
        public Student Student { get; set; }

        [Required]
        [StringLength(100)]
        public string PersonName { get; set; }

        [Required]
        [StringLength(15)]
        public string CNIC { get; set; }

        [Required]
        [StringLength(50)]
        public string Relation { get; set; }

        public string? PersonPicture { get; set; }

        public string? CNICPicture { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string? CreatedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public string? ModifiedBy { get; set; }
    }
}
