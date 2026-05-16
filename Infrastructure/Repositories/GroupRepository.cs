using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Repositories
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
        public async Task<Group?> GetExisting1v1Room(int userId, int targetId)
        {
            return await _context.Groups
                .Include(g => g.GroupUsers)
                .Where(g => g.IsGroup == false)
                .FirstOrDefaultAsync(g => g.GroupUsers.Any(gu => gu.AccountId == userId)
                                       && g.GroupUsers.Any(gu => gu.AccountId == targetId));
        }
        public async Task CreateGroup(Domain.Entities.Group group)
        {
            _context.Groups.Add(group);
        }

        public async Task DeleteGroup(Domain.Entities.Group group)
        {
            _context.Groups.Remove(group);
        }

        public async Task<GroupUser> GetAccountFromGroup(int groupId, int accId)
        {
            return await _context.GroupUsers
                .Include(gu => gu.Account)
                .FirstOrDefaultAsync(gu => gu.GroupId == groupId && gu.AccountId == accId);

        }

        public async Task<Domain.Entities.Group> GetGroupById(int groupId)
        {
            return await _context.Groups
                .Include(g => g.GroupUsers)
                    .ThenInclude(gu => gu.Account)
                        .ThenInclude(a => a.Avatars)
                .Include(g => g.Messages)
                .Include(g => g.Images)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);
        }

        public async Task<List<Domain.Entities.Group>> GetGroupsByAccountId(int accountId)
        {
            return await _context.Groups
                .Include(g => g.GroupUsers)
                    .ThenInclude(gu => gu.Account)
                        .ThenInclude(a => a.Avatars)
                .Include(g => g.Messages)
                .Include(g => g.Images)
                .Where(g => g.GroupUsers.Any(gu => gu.AccountId == accountId))
                .ToListAsync();
        }

        public async Task KickMemberFromGroup(GroupUser groupUser)
        {
            _context.GroupUsers.Remove(groupUser);
        }

        public async Task UpdateGroup(Domain.Entities.Group group)
        {
            _context.Groups.Update(group);
        }

        public async Task<List<GroupUser>> GetUsersInGroup(int groupId)
        {
            return await _context.GroupUsers
                .Include(gu => gu.Account)
                    .ThenInclude(a => a.Avatars)
                .Where(gu => gu.GroupId == groupId)
                .ToListAsync();
        }

        public async Task<List<Photo>> GetPhotosInGroup(int groupId)
        {
            return await _context.Photos
                .Include(p => p.Message)
                    .ThenInclude(m => m.Group)
                .Include(p => p.Message)
                    .ThenInclude(m => m.Account)
                        .ThenInclude(a => a.Avatars)
                .Where(p => p.Message.GroupId == groupId)
                .OrderByDescending(p => p.Message.SentAt)
                .ToListAsync();
        }
    }
}
