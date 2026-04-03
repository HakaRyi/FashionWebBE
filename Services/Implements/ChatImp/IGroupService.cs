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
        Task<int> CreateGroup2User(int targetUserId);
        Task AddMemberToGroup(int groupId, int userId);
        Task KickMemberToGroup(int groupId, int userId);
        Task UpdateGroup(int groupId, EditGroupRequest request);
        Task DeleteGroup(int groupId);
        Task<int?> CheckExisting1v1Group(int targetUserId);
    }
}
