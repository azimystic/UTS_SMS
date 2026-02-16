using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTS_SMS.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string SenderId { get; set; }

        [ForeignKey("SenderId")]
        public ApplicationUser Sender { get; set; }

        [Required]
        [StringLength(500)]
        public string Subject { get; set; }

        [Required]
        public string Body { get; set; } // HTML supported

        [Required]
        public DateTime SentDate { get; set; } = DateTime.Now;

        public int CampusId { get; set; }

        [ForeignKey("CampusId")]
        public Campus Campus { get; set; }

        // Navigation properties
        public ICollection<MessageRecipient> Recipients { get; set; } = new List<MessageRecipient>();
        public ICollection<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
    }
}
