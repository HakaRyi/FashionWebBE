using Domain.Entities;

namespace Domain.Interfaces

{
    public interface IGroupRepository
    {
        Task<Group> GetGroupById(int groupId);
        Task<List<Group>> GetGroupsByAccountId(int accountId);
        Task CreateGroup(Group group);
        Task AddMemberToGroup(GroupUser groupUser);
        Task<GroupUser> GetAccountFromGroup(int groupId, int accId);
        Task<bool> CheckIsRoomExist(int userId, int targetId);
        Task KickMemberFromGroup(GroupUser groupUser);
        Task UpdateGroup(Group group);
        Task DeleteGroup(Group group);
        Task<Group?> GetExisting1v1Room(int userId, int targetId);
        Task<List<GroupUser>> GetUsersInGroup(int groupId);
        Task<List<Photo>> GetPhotosInGroup(int groupId);
    }
}
