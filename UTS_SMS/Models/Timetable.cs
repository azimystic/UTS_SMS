// Timetable.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public class Timetable
    {
        public int Id { get; set; }

        [Required]
        public int ClassId { get; set; }
        public Class Class { get; set; }

        [Required]
        public int SectionId { get; set; }
        public ClassSection Section { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public DateTime StartTime { get; set; }

        [Required]
        [Range(1, 10)]
        public int NumberOfLectures { get; set; }

        [Required]
        [Range(1, 120)]
        public int LectureDuration { get; set; } // in minutes

        [Range(0, 60)]
        public int BreakDuration { get; set; } // in minutes (for backward compatibility)

        [Range(0, 10)]
        public int BreakAfterPeriod { get; set; } // After which period break comes (for backward compatibility)

        [Range(0, 60)]
        public int ZeroPeriodDuration { get; set; } // in minutes

        [DataType(DataType.Time)]
        public DateTime? ZeroPeriodStartTime { get; set; }

        [DataType(DataType.Time)]
        public DateTime? BreakStartTime { get; set; } // (for backward compatibility)

        public bool IsActive { get; set; } = true;
        public int CampusId { get; set; }
        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }

        // Navigation properties
        public ICollection<TimetableSlot> TimetableSlots { get; set; }
        public ICollection<TimetableBreak> TimetableBreaks { get; set; }
    }

    public class TimetableSlot
    {
        public int Id { get; set; }

        [Required]
        public int TimetableId { get; set; }
        public Timetable Timetable { get; set; }

        [Required]
        public int DayOfWeek { get; set; } // 1=Monday, 2=Tuesday, etc.

        [Required]
        public int PeriodNumber { get; set; } // 0=Zero period, 999=Break, 1-N=Regular periods

        [DataType(DataType.Time)]
        public DateTime StartTime { get; set; }

        [DataType(DataType.Time)]
        public DateTime EndTime { get; set; }

        public int? TeacherAssignmentId { get; set; }
        public TeacherAssignment TeacherAssignment { get; set; }

        public bool IsBreak { get; set; } = false;
        public bool IsZeroPeriod { get; set; } = false;

        [StringLength(100)]
        public string CustomTitle { get; set; } // For breaks, zero period, etc.
      
    }

    public class TimetableBreak
    {
        public int Id { get; set; }

        [Required]
        public int TimetableId { get; set; }
        public Timetable Timetable { get; set; }

        [Required]
        [Range(1, 10)]
        public int AfterPeriod { get; set; } // Break comes after this period

        [Required]
        [Range(1, 120)]
        public int Duration { get; set; } // Duration in minutes

        [StringLength(50)]
        public string Title { get; set; } // e.g., "Morning Break", "Lunch Break"
    }
}