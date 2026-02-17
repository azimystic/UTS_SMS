namespace UTS_SMS.ViewModels
{
    public class AiChatViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public int? StudentId { get; set; }
        public int? CampusId { get; set; }
        public List<SuggestedQuestion> SuggestedQuestions { get; set; } = new();
    }

    public class SuggestedQuestion
    {
        public string Icon { get; set; } = "fas fa-question-circle";
        public string Text { get; set; } = string.Empty;
    }
}
