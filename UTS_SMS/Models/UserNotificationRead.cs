using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTS_SMS.Models
{
    /// <summary>
    /// Tracks which users have read which notifications
    /// This allows the same notification to be unread for one user but read for another
    /// </summary>
    public class UserNotificationRead
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int NotificationId { get; set; }

        [ForeignKey("NotificationId")]
        public Notification Notification { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        public DateTime ReadAt { get; set; } = DateTime.Now;
    }
}
