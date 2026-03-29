using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.ChatImp;
using Services.Request.GroupReq;

namespace WebAPIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroupController : ControllerBase
    {
        private readonly IGroupService _groupService;
        public GroupController(IGroupService groupService)
        {
            _groupService = groupService;
        }
        [HttpGet("get-my-groups")]
        [Authorize]
        public async Task<IActionResult> GetGroups()
        {
            var result = await _groupService.GetMyGroupList();
            return Ok(result);

        }
        [HttpPost("create-group")]
        [Authorize]
        public async Task<IActionResult> CreateGroup([FromForm] GroupRequest request)
        {
            await _groupService.CreateGroup(request);
            return Ok(new
            {
                message = "create success"
            });

        }
        [HttpPut("update-group/{groupId}")]
        [Authorize]
        public async Task<IActionResult> UpdateGroup([FromRoute] int groupId, EditGroupRequest request)
        {
            await _groupService.UpdateGroup(groupId, request);
            return Ok(new
            {
                message = "update success"
            });

        }
        [HttpPost("create-1v1-room/{targetUserId}")]
        [Authorize]
        public async Task<IActionResult> CreateRoom1v1([FromRoute] int targetUserId)
        {
            await _groupService.CreateGroup2User(targetUserId);
            return Ok(new
            {
                message = "create 1v1 success"
            });

        }
        [HttpPost("add-member-to-group/{groupId}/{targetUserId}")]
        [Authorize]
        public async Task<IActionResult> AddMember([FromRoute] int targetUserId, int groupId)
        {
            await _groupService.AddMemberToGroup(groupId, targetUserId);
            return Ok(new
            {
                message = "add success"
            });

        }
        [HttpDelete("kick-member-from-group/{groupId}/{targetUserId}")]
        [Authorize]
        public async Task<IActionResult> KickMember([FromRoute] int targetUserId, int groupId)
        {
            await _groupService.KickMemberToGroup(groupId, targetUserId);
            return Ok(new
            {
                message = "kick success"
            });

        }
        [HttpDelete("delete-group/{groupId}")]
        [Authorize]
        public async Task<IActionResult> DeleteRoom([FromRoute] int groupId)
        {
            await _groupService.DeleteGroup(groupId);
            return Ok(new
            {
                message = "delete success"
            });

        }
    }
}
