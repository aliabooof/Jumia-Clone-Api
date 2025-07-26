using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.Dtos.ChatDTos
{
    public class SendMessageDto
    {
        public Guid ChatId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "Text";
    }
}
