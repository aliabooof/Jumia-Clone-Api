using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Domain.Models
{
    public class Chat
    {
        public Guid Id { get; set; }
        [ForeignKey("User")]
        public string UserId { get; set; } 
        public string UserName { get; set; } 
        public string UserEmail { get; set; } 
        public ChatStatus Status { get; set; } = ChatStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }
        public string? AdminId { get; set; }
        public string? AdminName { get; set; }

        public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public virtual AppUser User { get; set; } 
    }
    public enum ChatStatus
    {
        Active,
        Closed,
        Pending
    }
}
