using Application.Interfaces;
using Application.Request.MessageReq;
using Application.Request.PostReq;
using Application.Services.ChatImp;
using Application.Services.PostImp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Services;

namespace Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _service;
        private readonly IAccountService accountService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IChatShareService _chatShareService;

        public ChatController(IChatService service, IAccountService accountService, ICurrentUserService currentUserService, IChatShareService chatShareService)
        {
            _service = service;
            _chatShareService = chatShareService;
            this.accountService = accountService;
            _currentUserService = currentUserService;
        }

        [HttpPost("send/{groupId}")]
        [Authorize]
        public async Task<IActionResult> SendMessage([FromRoute] int groupId, [FromForm] SendMessageRequest request)
        {

            await _service.SendMessage(groupId, request);
            var user = await accountService.GetAccountByMe();
            return Ok(new
            {
                message = $"the message with content {request.content} is sent by {user.Username}"
            });
        }

        [HttpPut("recall-msg/{messageId}")]
        [Authorize]
        public async Task<IActionResult> RecallMessage([FromRoute] int messageId)
        {

            await _service.RecallMessage(messageId);
            return Ok(new
            {
                message = "recall successfully"
            });
        }

        [HttpGet("chat-history/{groupId}")]
        [Authorize]
        public async Task<IActionResult> HistoryChat([FromRoute] int groupId)
        {

            var result = await _service.GetHistoryMessage(groupId);
            return Ok(result);
        }

        [HttpPut("update/{messageId}/")]
        [Authorize]
        public async Task<IActionResult> UpdateMessage([FromRoute] int messageId, EditMessageRequest request)
        {

            await _service.UpdateMessage(messageId, request);
            return Ok(new
            {
                message = "the message with update sucessfully"
            });
        }

        [HttpDelete("delete/{messageId}/")]
        [Authorize]
        public async Task<IActionResult> DeleteMessage([FromRoute] int messageId)
        {

            await _service.DeleteMessage(messageId);
            return Ok(new
            {
                message = "the message with delete sucessfully"
            });
        }

        [HttpGet("get-pinned-msg/{groupId}")]
        [Authorize]
        public async Task<IActionResult> GetPinnedMsgByGroupId([FromRoute] int groupId)
        {

            var result = await _service.GetPinnedMessagesByGroupId(groupId);
            return Ok(result);
        }

        [HttpPost("add-message-reaction/{messageId}")]
        [Authorize]
        public async Task<IActionResult> AddReactMsg([FromRoute] int messageId, string type)
        {

            await _service.AddReaction(messageId, type);
            return Ok(new
            {
                message = "add successfully"
            });
        }

        [HttpPost("pin-msg/{messageId}/{groupId}")]
        [Authorize]
        public async Task<IActionResult> PinMsg([FromRoute] int messageId, int groupId)
        {

            await _service.PinMessage(messageId, groupId);
            return Ok(new
            {
                message = "pin successfully"
            });
        }

        [HttpDelete("unpin-msg/{pinMsgId}")]
        [Authorize]
        public async Task<IActionResult> UnPinMsg([FromRoute] int pinMsgId)
        {

            await _service.UnPinMessage(pinMsgId);
            return Ok(new
            {
                message = "unpin successfully"
            });
        }

        [HttpGet("get-reacts-by-message/{messId}")]
        [Authorize]
        public async Task<IActionResult> GetAllReactByMessage([FromRoute] int messId)
        {
            var result = await _service.GetReactorByMessId(messId);
            if (result == null)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("consult/{itemId}")]
        [Authorize]
        public async Task<IActionResult> ConsultItem(int itemId)
        {
            var groupId = await _service.SendConsultationRequest(itemId);
            return Ok(new { groupId = groupId });
        }

        [HttpPost("share-post")]
        [Authorize]
        public async Task<IActionResult> SharePostToChat([FromBody] SharePostToChatRequest request)
        {
            await _chatShareService.SharePostAsync(request);

            return Ok(new
            {
                message = "Post shared successfully."
            });
        }
    }
}
