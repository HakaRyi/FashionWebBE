using Microsoft.AspNetCore.Http;

namespace Application.Request.GroupReq
{
    public class GroupRequest
    {
        public string? Name { get; set; }
        public IFormFile? Avatar { get; set; }
        public List<int>? MemberIds { get; set; }
    }
    public class EditGroupRequest
    {
        public string? Name { get; set; }
        public IFormFile? Avatar { get; set; }
    }
}
