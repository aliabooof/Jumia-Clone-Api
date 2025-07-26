using Jumia_Api.Application.Interfaces;
using Jumia_Api.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jumia_Api.Infrastructure.Hubs
{
  
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

            if (userId != null)
            {
                // Add user to their personal group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");

                // Add admins to admin group
                if (userRole == "Admin")
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
                }

                // Get user's active chat and join chat group
                if (userRole != "Admin")
                {
                    var userChat = await _chatService.GetUserChatAsync(userId);
                    if (userChat != null)
                    {


                        
                        await Groups.AddToGroupAsync(Context.ConnectionId, $"Chat_{userChat.FirstOrDefault(c => c.Status == ChatStatus.Active.ToString()).UserId}");

                    }
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

            if (userId != null)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");

                if (userRole == "Admin")
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinChatGroup(string chatId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Chat_{chatId}");
        }

        public async Task LeaveChatGroup(string chatId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Chat_{chatId}");
        }

        public async Task MarkMessagesAsRead(string chatId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null && Guid.TryParse(chatId, out var chatGuid))
            {
                await _chatService.MarkMessagesAsReadAsync(chatGuid, userId);
            }
        }
    }
}
