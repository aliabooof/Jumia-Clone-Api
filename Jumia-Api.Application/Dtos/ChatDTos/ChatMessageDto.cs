using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.Dtos.ChatDTos
{
    public class ChatMessageDto
    {
        public Guid Id { get; set; }
        public Guid ChatId { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsFromAdmin { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
    }
}
