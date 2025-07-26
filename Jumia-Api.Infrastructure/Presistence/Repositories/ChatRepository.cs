using Jumia_Api.Domain.Interfaces.Repositories;
using Jumia_Api.Domain.Models;
using Jumia_Api.Infrastructure.Presistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Infrastructure.Presistence.Repositories
{
    public class ChatRepository : GenericRepo<Chat>, IChatRepository
    {
        

        public ChatRepository(JumiaDbContext context) : base(context)
        {
        }

        public async Task<Chat?> GetByIdAsync(Guid id)
        {
            return await _dbSet
                .Include(c => c.Messages.OrderBy(m => m.SentAt))
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Chat>> GetByUserIdAsync(string userId)
        {
            return await _dbSet
                .Include(c => c.Messages.OrderBy(m => m.SentAt))
                .Where(c=>c.UserId==userId).ToListAsync();
        }

        public async Task<IEnumerable<Chat>> GetAllActiveChatsAsync()
        {
            return await _dbSet
                .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .Where(c => c.Status != ChatStatus.Closed)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Chat>> GetChatsByAdminIdAsync(string adminId)
        {
            return await _dbSet
                .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .Where(c => c.AdminId == adminId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Chat> CreateAsync(Chat chat)
        {
            _dbSet.Add(chat);
            await _context.SaveChangesAsync();
            return chat;
        }

        public async Task<Chat> UpdateAsync(Chat chat)
        {
            _dbSet.Update(chat);
            await _context.SaveChangesAsync();
            return chat;
        }

        public async Task DeleteAsync(Guid id)
        {
            var chat = await _dbSet.FindAsync(id);
            if (chat != null)
            {
                _dbSet.Remove(chat);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<ChatMessage>> GetChatMessagesAsync(Guid chatId, int page = 1, int pageSize = 50)
        {
            return await _context.ChatMessages
                .Where(m => m.ChatId == chatId)
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        public async Task<ChatMessage> AddMessageAsync(ChatMessage message)
        {
            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task MarkMessagesAsReadAsync(Guid chatId, string userId)
        {
            var messages = await _context.ChatMessages
                .Where(m => m.ChatId == chatId && m.SenderId != userId && !m.IsRead)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadMessageCountAsync(Guid chatId, string userId)
        {
            return await _context.ChatMessages
                .CountAsync(m => m.ChatId == chatId && m.SenderId != userId && !m.IsRead);
        }
    }
}
