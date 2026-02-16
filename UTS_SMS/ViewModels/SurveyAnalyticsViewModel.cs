namespace SMS.ViewModels
{
    public class SurveyAnalyticsViewModel
    {
        public int TotalSubmissions { get; set; }
        public List<QuestionAverageViewModel> QuestionAverages { get; set; } = new List<QuestionAverageViewModel>();
    }

    public class QuestionAverageViewModel
    {
        public string QuestionText { get; set; } = string.Empty;
        public int QuestionOrder { get; set; }
        public double AverageScore { get; set; }
        public int TotalResponses { get; set; }
    }

    public class MissingStudentViewModel
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        public string RollNo { get; set; } = string.Empty;
    }
}
