using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.Dtos.ChatDTos
{
    public class ChatDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string? AdminId { get; set; }
        public string? AdminName { get; set; }
        public int UnreadCount { get; set; }
        public ChatMessageDto? LastMessage { get; set; }
    }
}
