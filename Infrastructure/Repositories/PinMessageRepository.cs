using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Repositories
{
    public class PinMessageRepository : IPinMessageRepository
    {
        private readonly FashionDbContext _context;
        public PinMessageRepository(FashionDbContext context)
        {
            _context = context;
        }

        public async Task AddPinnedMessageAsync(PinnedMessage pinnedMessage)
        {
            _context.PinnedMessages.Add(pinnedMessage);
        }


        public async Task<PinnedMessage> GetPinnedMessageAsync(int pinnedMsgId)
        {
            return await _context.PinnedMessages
                .Include(pm => pm.AccountPinned)
                .Include(pm => pm.Group)
                .Include(pm => pm.Message)
                .FirstOrDefaultAsync(pm => pm.PinnedMsgId == pinnedMsgId);
        }

        public async Task<List<PinnedMessage>> GetPinnedMessagesByGroupIdAsync(int groupId)
        {
            return await _context.PinnedMessages
                 .Include(pm => pm.AccountPinned)
                 .Include(pm => pm.Group)
                 .Include(pm => pm.Message)
                 .Where(pm => pm.GroupId == groupId)
                 .ToListAsync();
        }

        public async Task RemovePinnedMessageAsync(PinnedMessage pinnedMessage)
        {
            _context.PinnedMessages.Remove(pinnedMessage);
        }
    }
}
