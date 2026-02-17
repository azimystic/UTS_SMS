using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTS_SMS.Models
{
    public enum ChatRole
    {
        User,
        Assistant,
        Tool
    }

    public class AiChatMessage
    {
        public int Id { get; set; }

        [Required]
        public int ConversationId { get; set; }

        [ForeignKey("ConversationId")]
        public AiChatConversation? Conversation { get; set; }

        [Required]
        public ChatRole Role { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ToolName { get; set; }

        /// <summary>
        /// JSON array of source citations: [{fileName, filePath, pageNumber, chapterName}]
        /// </summary>
        public string? Sources { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
