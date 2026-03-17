using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Entities;

namespace Repositories.Repos.GroupRepos
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
    }
}
