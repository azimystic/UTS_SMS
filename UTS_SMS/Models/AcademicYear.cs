using System.ComponentModel.DataAnnotations;

namespace SMS.Models
{
    public class AcademicYear
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int Year { get; set; }
    }
}
