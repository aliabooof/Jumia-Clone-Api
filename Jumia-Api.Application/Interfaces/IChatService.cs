using Jumia_Api.Application.Dtos.ChatDTos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.Interfaces
{
    public interface IChatService
    {
        Task<ChatDto> CreateChatAsync(CreateChatDto createChatDto);
        Task<ChatDto?> GetChatAsync(Guid chatId);
        Task<IEnumerable<ChatDto>> GetUserChatAsync(string userId);
        Task<IEnumerable<ChatDto>> GetAllActiveChatsAsync();
        Task<IEnumerable<ChatDto>> GetAdminChatsAsync(string adminId);
        Task<ChatMessageDto> SendMessageAsync(SendMessageDto sendMessageDto, string senderId, string senderName, bool isFromAdmin);
        Task<IEnumerable<ChatMessageDto>> GetChatMessagesAsync(Guid chatId, int page = 1, int pageSize = 50);
        Task<ChatDto> AssignChatToAdminAsync(Guid chatId, string adminId, string adminName);
        Task<ChatDto> CloseChatAsync(Guid chatId);
        Task MarkMessagesAsReadAsync(Guid chatId, string userId);
    }
}
