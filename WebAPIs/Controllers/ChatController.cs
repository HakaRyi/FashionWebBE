using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.DTO.Request;
using Services.AI;
using Services.Implements.AccountService;
using Services.Implements.ChatImp;

namespace WebAPIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _service;
        private readonly IAccountService accountService;
        public ChatController(IChatService service, IAccountService accountService)
        {
            _service = service;
            this.accountService = accountService;
        }
        [HttpPost("send/{groupId}")]
        [Authorize]
        public async Task<IActionResult> SendMessage([FromRoute] int groupId, SendMessageRequest request)
        {

            await _service.SendMessage(groupId,request);
            var user = await accountService.GetAccountByMe() ;
            return Ok(new
            {
                message = $"the message with content {request.content} is sent by {user.Username}"
            });
        }
        [HttpPost("recall-msg/{groupId}")]
        [Authorize]
        public async Task<IActionResult> RecallMessage([FromRoute] int groupId, SendMessageRequest request)
        {

            await _service.SendMessage(groupId, request);
            var user = await accountService.GetAccountByMe();
            return Ok(new
            {
                message = $"the message with content {request.content} is sent by {user.Username}"
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


    }
}
