using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Domain.Entities;

namespace Infrastructure.Repositories
{
    public class ChatRepository : IChatRepository
    {
        private readonly FashionDbContext _context;
        public ChatRepository(FashionDbContext context)
        {
            _context = context;
        }
        public async Task<Group> GetGroupById(int id)
        {
            var entity = await _context.Groups.FirstOrDefaultAsync(u => u.GroupId == id);
            return entity ?? new Group();
        }
        public async Task<Account> GetAccountById(int id)
        {
            var entity = await _context.Users
                .Include(u => u.Avatars)
                .FirstOrDefaultAsync(u => u.Id == id);
            return entity ?? new Account();
        }

        public async Task AddMessage(Message message)
        {
            _context.Messages.Add(message);
        }
        public async Task DeleteMessage(Message message)
        {
            _context.Messages.Remove(message);
        }
        public async Task EditMessage(Message message)
        {
            _context.Messages.Update(message);
        }

        public async Task<List<Message>> GetHistoryMessages(int groupId)
        {
            return await _context.Messages
                .Include(m => m.Photos)
                .Include(m => m.MessReactions)
                .Include(m => m.ReplyToMessage)
                .Include(m => m.Account)
                .Include(m => m.Group)
                .Where(m => m.GroupId == groupId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        public async Task<Message> GetMessageById(int id)
        {
            return await _context.Messages
                .Include(m => m.Photos)
                .Include(m => m.MessReactions)
                .Include(m => m.ReplyToMessage)
                .Include(m => m.Account)
                .Include(m => m.Group)
                .FirstOrDefaultAsync(m => m.MessageId == id) ?? new Message();
        }
        public async Task AddOrUpdateReaction(MessReaction reaction)
        {
            var existing = await _context.MessReactions
                .FirstOrDefaultAsync(r => r.MessageId == reaction.MessageId && r.AccountReactId == reaction.AccountReactId);

            if (existing != null)
            {
                existing.Type = reaction.Type;
                _context.MessReactions.Update(existing);
            }
            else
            {
                _context.MessReactions.Add(reaction);
            }
        }
        public async Task<List<MessReaction>> GetAllReactionByMessageiD(int messageId)
        {
            return await _context.MessReactions
                .Include(r => r.AccountReact)
                .Include(r => r.Message)
                .Where(r => r.MessageId == messageId)
                .OrderByDescending(r => r.MessageId)
                .ToListAsync();
        }
    }
}
