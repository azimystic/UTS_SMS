using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public class ClassFee
    {
        [Key]
        public int Id { get; set; }

        [Required]
         public int ClassId { get; set; }
        [ForeignKey("ClassId")]
        public Class Class { get; set; }

        [Display(Name = "Tuition Fee")]
        [Range(0, double.MaxValue, ErrorMessage = "Tuition fee must be a positive value")]
        public decimal TuitionFee { get; set; }

        [Display(Name = "Admission Fee")]
        [Range(0, double.MaxValue, ErrorMessage = "Admission fee must be a positive value")]
        public decimal AdmissionFee { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        public string CreatedBy { get; set; }

        public string ModifiedBy { get; set; }
        public int CampusId { get; set; }
        public Campus Campus { get; set; }

    }
}
