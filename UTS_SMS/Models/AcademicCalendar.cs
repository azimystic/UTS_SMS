using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTS_SMS.Models
{
    public class AcademicCalendar
    {
        public int Id { get; set; }

        [Required]
        public int CampusId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        [StringLength(200)]
        public string HolidayName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        [StringLength(20)]
        public string HolidayType { get; set; } // National, Religious, Academic, Local

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("CampusId")]
        public virtual Campus Campus { get; set; }
    }

    public class AcademicCalendarViewModel
    {
        public int Id { get; set; }
        public int CampusId { get; set; }
        public string CampusName { get; set; }
        public DateTime Date { get; set; }
        public string HolidayName { get; set; }
        public string Description { get; set; }
        public string HolidayType { get; set; }
        public bool IsActive { get; set; }
    }

    public class MonthlyCalendarViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; }
        public List<AcademicCalendar> Holidays { get; set; } = new List<AcademicCalendar>();
        public List<CalendarDay> CalendarDays { get; set; } = new List<CalendarDay>();
    }

    public class CalendarDay
    {
        public int Day { get; set; }
        public bool IsHoliday { get; set; }
        public List<AcademicCalendar> Holidays { get; set; } = new List<AcademicCalendar>();
        public bool IsWeekend { get; set; }
        public bool IsCurrentMonth { get; set; }
    }
}