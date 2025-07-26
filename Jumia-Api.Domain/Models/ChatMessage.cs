using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Domain.Models
{
    public class ChatMessage
    {
        public Guid Id { get; set; }
        [ForeignKey("Chat")]
        public Guid ChatId { get; set; }
        [ForeignKey("User")]
        public string SenderId { get; set; } 
        public string SenderName { get; set; } 
        public string Message { get; set; } 
        public MessageType Type { get; set; } = MessageType.Text;
        public bool IsFromAdmin { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;

        public virtual Chat Chat { get; set; } 
        public virtual AppUser User { get; set; }
    }

    public enum MessageType
    {
        Text,
        Image,
        File,
        System
    }
}
