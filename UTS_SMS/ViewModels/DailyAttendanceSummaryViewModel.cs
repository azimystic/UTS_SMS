namespace UTS_SMS.ViewModels
{
    public class DailyAttendanceSummaryViewModel
    {
        public int ClassID { get; set; }
        public int SectionID { get; set; }
        public string Class { get; set; } // For display purposes
        public string Section { get; set; } // For display purposes
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Leave { get; set; }
        public int Late { get; set; }
        public int TotalStudents { get; set; }

        public int TotalMarked => Present + Absent + Leave + Late;
        public double AttendancePercentage => TotalMarked > 0 ? Math.Round(((double)Present + (double)Late) / TotalMarked * 100, 1) : 0;
    }
}