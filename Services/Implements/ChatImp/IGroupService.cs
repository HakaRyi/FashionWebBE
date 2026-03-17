using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Services.Request.GroupReq;
using Services.Response.GroupResp;

namespace Services.Implements.ChatImp
{
    public interface IGroupService
    {
        Task<GroupResponse> GetGroupById(int groupId);
        Task<List<GroupResponse>> GetGroupsByAccountId(int accountId);
        Task<List<GroupResponse>> GetMyGroupList();
        Task CreateGroup(GroupRequest request);
        Task CreateGroup2User(int targetUserId);
        Task AddMemberToGroup(int groupId, int userId);
        Task KickMemberToGroup(int groupId, int userId);
        Task UpdateGroup(int groupId,EditGroupRequest request);
        Task DeleteGroup(int groupId);
    }
}
