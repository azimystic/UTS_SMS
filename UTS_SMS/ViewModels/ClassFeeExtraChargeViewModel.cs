using System.ComponentModel.DataAnnotations;

namespace SMS.ViewModels
{
    public class ClassFeeExtraChargeViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Charge name is required")]
        [StringLength(100)]
        [Display(Name = "Charge Name")]
        public string ChargeName { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Amount must be a positive value")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public string Category { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public List<int> ExcludedStudentIds { get; set; } = new List<int>();
        public List<int> SelectedStudentIds { get; set; } = new List<int>();
    }

    public class ClassFeeWithExtraChargesViewModel
    {
        public int Id { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public decimal TuitionFee { get; set; }
        public decimal AdmissionFee { get; set; }
        public decimal MiscallaneousCharges { get; set; }
        
        public List<ClassFeeExtraChargeViewModel> ExtraCharges { get; set; } = new List<ClassFeeExtraChargeViewModel>();
        
        // For apply to other classes
        public bool ApplyToAllClasses { get; set; }
        public List<int> SelectedClassIds { get; set; } = new List<int>();
    }
}
