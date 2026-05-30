using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.Chat;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("rooms")]
        [ProducesResponseType(typeof(Result<ChatRoomResponse>), 200)]
        public async Task<IActionResult> CreateOrGetRoom([FromBody] CreateChatRoomRequest request)
        {
            var userId = User.GetUserId();
            var result = await _chatService.CreateOrGetRoomAsync(userId, request);
            return Ok(result);
        }

        [HttpGet("rooms")]
        [ProducesResponseType(typeof(Result<List<ChatRoomResponse>>), 200)]
        public async Task<IActionResult> GetRooms()
        {
            var userId = User.GetUserId();
            var result = await _chatService.GetRoomsAsync(userId);
            return Ok(result);
        }

        [HttpGet("rooms/{roomId:guid}/messages")]
        [ProducesResponseType(typeof(Result<PagedResult<ChatMessageResponse>>), 200)]
        public async Task<IActionResult> GetMessages(
            Guid roomId,
            [FromQuery] ChatMessageFilterRequest filter)
        {
            var userId = User.GetUserId();
            var result = await _chatService.GetMessagesAsync(userId, roomId, filter);
            return Ok(result);
        }

        [HttpPost("rooms/{roomId:guid}/messages")]
        [ProducesResponseType(typeof(Result<ChatMessageResponse>), 200)]
        public async Task<IActionResult> SendMessage(
            Guid roomId,
            [FromBody] SendMessageRequest request)
        {
            var userId = User.GetUserId();
            var result = await _chatService.SendMessageAsync(userId, roomId, request);
            return Ok(result);
        }
    }
}
