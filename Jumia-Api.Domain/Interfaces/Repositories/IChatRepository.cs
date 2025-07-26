using Jumia_Api.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Domain.Interfaces.Repositories
{
    public interface IChatRepository
    {
        Task<Chat?> GetByIdAsync(Guid id);
        Task<IEnumerable<Chat>> GetByUserIdAsync(string userId);
        Task<IEnumerable<Chat>> GetAllActiveChatsAsync();
        Task<IEnumerable<Chat>> GetChatsByAdminIdAsync(string adminId);
        Task<Chat> CreateAsync(Chat chat);
        Task<Chat> UpdateAsync(Chat chat);
        Task DeleteAsync(Guid id);
        Task<IEnumerable<ChatMessage>> GetChatMessagesAsync(Guid chatId, int page = 1, int pageSize = 50);
        Task<ChatMessage> AddMessageAsync(ChatMessage message);
        Task MarkMessagesAsReadAsync(Guid chatId, string userId);
        Task<int> GetUnreadMessageCountAsync(Guid chatId, string userId);
    }
}
