using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.GroupRepos
{
    public class GroupRepository : IGroupRepository
    {
        private readonly FashionDbContext _context;
        public GroupRepository(FashionDbContext context)
        {
            _context = context;
        }

        public async Task AddMemberToGroup(GroupUser groupUser)
        {
            _context.GroupUsers.Add(groupUser);
        }

        public async Task<bool> CheckIsRoomExist(int userId, int targetId)
        {
            return await _context.Groups
         .Where(g => g.IsGroup == false)
         .AnyAsync(g => g.GroupUsers.Any(gu => gu.AccountId == userId)
                     && g.GroupUsers.Any(gu => gu.AccountId == targetId));
        }

        public async Task CreateGroup(Entities.Group group)
        {
            _context.Groups.Add(group);
        }

        public async Task DeleteGroup(Entities.Group group)
        {
            _context.Groups.Remove(group);
        }

        public async Task<GroupUser> GetAccountFromGroup(int groupId, int accId)
        {
            return await _context.GroupUsers
                .Include(gu => gu.Account)
                .FirstOrDefaultAsync(gu => gu.GroupId == groupId && gu.AccountId == accId);

        }

        public async Task<Entities.Group> GetGroupById(int groupId)
        {
            return await _context.Groups
                .Include(g => g.GroupUsers)
                    .ThenInclude(gu => gu.Account)
                        .ThenInclude(a => a.Avatars)
                .Include(g => g.Messages)
                .Include(g => g.Images)
                .FirstOrDefaultAsync(g=>g.GroupId==groupId);
        }

        public async Task<List<Entities.Group>> GetGroupsByAccountId(int accountId)
        {
            return await _context.Groups
                .Include(g => g.GroupUsers)
                    .ThenInclude(gu => gu.Account)
                        .ThenInclude(a => a.Avatars)
                .Include(g => g.Messages)
                .Include(g => g.Images)
                .Where(g => g.GroupUsers.Any(gu=>gu.AccountId ==accountId))
                .ToListAsync();
        }

        public async Task KickMemberFromGroup(GroupUser groupUser)
        {
            _context.GroupUsers.Remove(groupUser);
        }

        public async Task UpdateGroup(Entities.Group group)
        {
            _context.Groups.Update(group);
        }
    }
}
