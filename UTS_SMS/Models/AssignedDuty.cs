using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTS_SMS.Models
{
    public class AssignedDuty
    {
        public int Id { get; set; }

        public int? EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [Required]
        [StringLength(200)]
        public string DutyTitle { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? DutyType { get; set; } // Daily, Weekly, Monthly, Special, Event-based

        [Required]
        [StringLength(50)]
        public string Priority { get; set; } // High, Medium, Low

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Assigned"; // Assigned, In Progress, Completed, Overdue, Cancelled

        [StringLength(1000)]
        public string? Instructions { get; set; }

        [StringLength(1000)]
        public string? CompletionNotes { get; set; }

        public DateTime? CompletedDate { get; set; }

        [Range(0, 100)]
        public int ProgressPercentage { get; set; } = 0;

        [Required]
        public int CampusId { get; set; }

        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }

        [StringLength(100)]
        public string? AssignedBy { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public bool IsActive { get; set; } = true;

        // Recurring duty properties
        public bool IsRecurring { get; set; } = false;

        [StringLength(50)]
        public string? RecurrencePattern { get; set; } // Daily, Weekly, Monthly

        public int? RecurrenceInterval { get; set; } // Every X days/weeks/months

        public DateTime? NextDueDate { get; set; }

        // Navigation property for students assigned to this duty
        public virtual ICollection<AssignedDutyStudent> AssignedStudents { get; set; } = new List<AssignedDutyStudent>();
    }

    // Junction table for many-to-many relationship between AssignedDuty and Student
    public class AssignedDutyStudent
    {
        public int Id { get; set; }

        [Required]
        public int AssignedDutyId { get; set; }
        [ForeignKey("AssignedDutyId")]
        public AssignedDuty AssignedDuty { get; set; }

        [Required]
        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public Student Student { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.Now;
        
        [StringLength(50)]
        public string Status { get; set; } = "Assigned"; // Assigned, In Progress, Completed, Not Started
        
        [StringLength(1000)]
        public string? StudentNotes { get; set; }
        
        public DateTime? CompletedDate { get; set; }
        
        [Range(0, 100)]
        public int ProgressPercentage { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }
}