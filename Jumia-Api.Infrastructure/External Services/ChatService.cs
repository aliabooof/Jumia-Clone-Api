using Jumia_Api.Application.Dtos.ChatDTos;
using Jumia_Api.Application.Interfaces;
using Jumia_Api.Domain.Interfaces.Repositories;
using Jumia_Api.Domain.Models;
using Jumia_Api.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Infrastructure.External_Services
{
    public class ChatService : IChatService
    {
        private readonly IChatRepository _chatRepository;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatService(IChatRepository chatRepository, IHubContext<ChatHub> hubContext)
        {
            _chatRepository = chatRepository;
            _hubContext = hubContext;
        }

        public async Task<ChatDto> CreateChatAsync(CreateChatDto createChatDto)
        {
            // Check if user already has an active chat
            var existingChat = await _chatRepository.GetByUserIdAsync(createChatDto.UserId);
            var existingChatFlag = existingChat?.FirstOrDefault(c => c.Status == ChatStatus.Active);
            if (existingChat != null && existingChatFlag.Status == ChatStatus.Active)
            {
                return MapToDto(existingChatFlag);
            }

            var chat = new Chat
            {
                Id = Guid.NewGuid(),
                UserId = createChatDto.UserId,
                UserName = createChatDto.UserName,
                UserEmail = createChatDto.UserEmail,
                Status = ChatStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            var createdChat = await _chatRepository.CreateAsync(chat);

            // Send initial message
            if (!string.IsNullOrEmpty(createChatDto.InitialMessage))
            {
                var initialMessage = new ChatMessage
                {
                    Id = Guid.NewGuid(),
                    ChatId = createdChat.Id,
                    SenderId = createChatDto.UserId,
                    SenderName = createChatDto.UserName,
                    Message = createChatDto.InitialMessage,
                    Type = MessageType.Text,
                    IsFromAdmin = false,
                    SentAt = DateTime.UtcNow
                };

                await _chatRepository.AddMessageAsync(initialMessage);
            }

            // Notify admins about new chat
            await _hubContext.Clients.Group("Admins").SendAsync("NewChatCreated", MapToDto(createdChat));

            return MapToDto(createdChat);
        }

        public async Task<ChatDto?> GetChatAsync(Guid chatId)
        {
            var chat = await _chatRepository.GetByIdAsync(chatId);
            return chat != null ? MapToDto(chat) : null;
        }

        public async Task<IEnumerable<ChatDto>> GetUserChatAsync(string userId)
        {
            var chat = await _chatRepository.GetByUserIdAsync(userId);
            return chat != null ? MapToChatListDto(chat) : null;
        }

        public async Task<IEnumerable<ChatDto>> GetAllActiveChatsAsync()
        {
            var chats = await _chatRepository.GetAllActiveChatsAsync();
            return chats.Select(MapToDto);
        }

        public async Task<IEnumerable<ChatDto>> GetAdminChatsAsync(string adminId)
        {
            var chats = await _chatRepository.GetChatsByAdminIdAsync(adminId);
            return chats.Select(MapToDto);
        }

        public async Task<ChatMessageDto> SendMessageAsync(SendMessageDto sendMessageDto, string senderId, string senderName, bool isFromAdmin)
        {
            var message = new ChatMessage
            {
                Id = Guid.NewGuid(),
                ChatId = sendMessageDto.ChatId,
                SenderId = senderId,
                SenderName = senderName,
                Message = sendMessageDto.Message,
                Type = Enum.Parse<MessageType>(sendMessageDto.Type),
                IsFromAdmin = isFromAdmin,
                SentAt = DateTime.UtcNow
            };

            var savedMessage = await _chatRepository.AddMessageAsync(message);

            // Update chat status to active if it was pending
            var chat = await _chatRepository.GetByIdAsync(sendMessageDto.ChatId);
            if (chat != null && chat.Status == ChatStatus.Pending)
            {
                chat.Status = ChatStatus.Active;
                await _chatRepository.UpdateAsync(chat);
            }

            var messageDto = MapToMessageDto(savedMessage);

            // Send message to chat participants
            await _hubContext.Clients.Group($"Chat_{sendMessageDto.ChatId}")
                .SendAsync("ReceiveMessage", messageDto);

            return messageDto;
        }

        public async Task<IEnumerable<ChatMessageDto>> GetChatMessagesAsync(Guid chatId, int page = 1, int pageSize = 50)
        {
            var messages = await _chatRepository.GetChatMessagesAsync(chatId, page, pageSize);
            return messages.Select(MapToMessageDto);
        }

        public async Task<ChatDto> AssignChatToAdminAsync(Guid chatId, string adminId, string adminName)
        {
            var chat = await _chatRepository.GetByIdAsync(chatId);
            if (chat == null) throw new ArgumentException("Chat not found");

            chat.AdminId = adminId;
            chat.AdminName = adminName;
            chat.Status = ChatStatus.Active;

            var updatedChat = await _chatRepository.UpdateAsync(chat);

            // Notify participants
            await _hubContext.Clients.Group($"Chat_{chatId}")
                .SendAsync("ChatAssigned", MapToDto(updatedChat));

            return MapToDto(updatedChat);
        }

        public async Task<ChatDto> CloseChatAsync(Guid chatId)
        {
            var chat = await _chatRepository.GetByIdAsync(chatId);
            if (chat == null) throw new ArgumentException("Chat not found");

            chat.Status = ChatStatus.Closed;
            chat.ClosedAt = DateTime.UtcNow;

            var updatedChat = await _chatRepository.UpdateAsync(chat);

            // Notify participants
            await _hubContext.Clients.Group($"Chat_{chatId}")
                .SendAsync("ChatClosed", MapToDto(updatedChat));

            return MapToDto(updatedChat);
        }

        public async Task MarkMessagesAsReadAsync(Guid chatId, string userId)
        {
            await _chatRepository.MarkMessagesAsReadAsync(chatId, userId);

            // Notify other participants
            await _hubContext.Clients.Group($"Chat_{chatId}")
                .SendAsync("MessagesRead", new { ChatId = chatId, UserId = userId });
        }

        private static ChatDto MapToDto(Chat chat)
        {
            return new ChatDto
            {
                Id = chat.Id,
                UserId = chat.UserId,
                UserName = chat.UserName,
                UserEmail = chat.UserEmail,
                Status = chat.Status.ToString(),
                CreatedAt = chat.CreatedAt,
                ClosedAt = chat.ClosedAt,
                AdminId = chat.AdminId,
                AdminName = chat.AdminName,
                LastMessage = chat.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault() != null
                    ? MapToMessageDto(chat.Messages.OrderByDescending(m => m.SentAt).First())
                    : null
            };
        }

        private static ChatMessageDto MapToMessageDto(ChatMessage message)
        {
            return new ChatMessageDto
            {
                Id = message.Id,
                ChatId = message.ChatId,
                SenderId = message.SenderId,
                SenderName = message.SenderName,
                Message = message.Message,
                Type = message.Type.ToString(),
                IsFromAdmin = message.IsFromAdmin,
                SentAt = message.SentAt,
                IsRead = message.IsRead
            };
        }

        private static IEnumerable<ChatDto> MapToChatListDto(IEnumerable<Chat> chat)
        {
            var chats = chat.Select(c => new ChatDto
            {
                Id = c.Id,
                UserId = c.UserId,
                UserName = c.UserName,
                UserEmail = c.UserEmail,
                Status = c.Status.ToString(),
                CreatedAt = c.CreatedAt,
                ClosedAt = c.ClosedAt,
                AdminId = c.AdminId,
                AdminName = c.AdminName,
                LastMessage = c.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault() != null
                    ? MapToMessageDto(c.Messages.OrderByDescending(m => m.SentAt).First())
                    : null
            }).ToList();


            return chats;
        }



    }
}
