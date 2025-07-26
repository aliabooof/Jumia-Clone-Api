using Jumia_Api.Application.Dtos.ChatDTos;
using Jumia_Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Jumia_Api.Api.Controllers
{
    public class init
    {
        public string InitialMessage { get; set; } = "";
    }



    [ApiController]
    [Route("api/[controller]")]
   
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
       
        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost]
        public async Task<ActionResult<ChatDto>> CreateChat([FromBody] init init )
        {
            var chatDto = GetUserDto();
            chatDto.InitialMessage = init.InitialMessage;
            try
            {
                var chat = await _chatService.CreateChatAsync(chatDto);
                return Ok(chat);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ChatDto>> GetChat(Guid id)
        {
            var chat = await _chatService.GetChatAsync(id);
            if (chat == null)
                return NotFound();

            return Ok(chat);
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<ChatDto>> GetUserChat(string userId)
        {
            var chat = await _chatService.GetUserChatAsync(userId);
            if (chat == null)
                return NotFound();

            return Ok(chat);
        }

        [HttpGet("my-chat")]
        public async Task<IActionResult> GetMyChat()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var chat = await _chatService.GetUserChatAsync(userId);
            if (chat == null)
                return NotFound();

            return Ok(chat);
        }

        [HttpGet("active")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ChatDto>>> GetActiveChats()
        {
            var chats = await _chatService.GetAllActiveChatsAsync();
            return Ok(chats);
        }

        [HttpGet("admin/{adminId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ChatDto>>> GetAdminChats(string adminId)
        {
            var chats = await _chatService.GetAdminChatsAsync(adminId);
            return Ok(chats);
        }

        [HttpGet("my-admin-chats")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ChatDto>>> GetMyAdminChats()
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (adminId == null)
                return Unauthorized();

            var chats = await _chatService.GetAdminChatsAsync(adminId);
            return Ok(chats);
        }

        [HttpPost("send-message")]
        public async Task<ActionResult<ChatMessageDto>> SendMessage([FromBody] SendMessageDto sendMessageDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown User";
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userId == null)
                return Unauthorized();

            try
            {
                var message = await _chatService.SendMessageAsync(
                    sendMessageDto,
                    userId,
                    userName,
                    userRole == "Admin"
                );
                return Ok(message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{chatId}/messages")]
        public async Task<ActionResult<IEnumerable<ChatMessageDto>>> GetChatMessages(
            Guid chatId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var messages = await _chatService.GetChatMessagesAsync(chatId, page, pageSize);
            return Ok(messages);
        }

        [HttpPut("{chatId}/assign")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ChatDto>> AssignChatToAdmin(Guid chatId)
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var adminName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Admin";

            if (adminId == null)
                return Unauthorized();

            try
            {
                var chat = await _chatService.AssignChatToAdminAsync(chatId, adminId, adminName);
                return Ok(chat);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{chatId}/close")]
        public async Task<ActionResult<ChatDto>> CloseChat(Guid chatId)
        {
            try
            {
                var chat = await _chatService.CloseChatAsync(chatId);
                return Ok(chat);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{chatId}/mark-read")]
        public async Task<ActionResult> MarkMessagesAsRead(Guid chatId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            await _chatService.MarkMessagesAsReadAsync(chatId, userId);
            return Ok();
        }

        private CreateChatDto GetUserDto()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown User";
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown Email";
            if (userId == null)
                throw new UnauthorizedAccessException("User not authenticated");
            return new CreateChatDto
            {
                UserId = userId,
                UserName = userName,
                UserEmail = userEmail
            };
        }
    }
}
