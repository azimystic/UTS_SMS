using UTS_SMS.Models;

namespace SMS.ViewModels
{
    // ViewModel for managing Academic Years per class (not section)
    public class ClassAcademicYearViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public string? CurrentAcademicYear { get; set; }
        public int CampusId { get; set; }
        public string CampusName { get; set; }
        public int SectionCount { get; set; }
    }

    // ViewModel for the Academic Year edit modal
    public class UpdateAcademicYearRequest
    {
        public int ClassId { get; set; }
        public string NewAcademicYear { get; set; }
    }

    // Legacy ViewModel for backward compatibility (deprecated)
    public class ClassSectionAcademicYearViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public int SectionId { get; set; }
        public string SectionName { get; set; }
        public string? CurrentAcademicYear { get; set; }
        public int CampusId { get; set; }
        public string CampusName { get; set; }
    }
}
