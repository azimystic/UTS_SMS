using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Models
{
    public class MessageAttachment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MessageId { get; set; }

        [ForeignKey("MessageId")]
        public Message Message { get; set; }

        [Required]
        [StringLength(500)]
        public string FileName { get; set; }

        [Required]
        [StringLength(1000)]
        public string FilePath { get; set; }

        [Required]
        [StringLength(100)]
        public string FileType { get; set; } // MIME type

        public long FileSize { get; set; } // in bytes

        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }
}
