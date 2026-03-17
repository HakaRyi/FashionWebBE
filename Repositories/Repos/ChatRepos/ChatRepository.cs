using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;
using Repositories.Repos.ChatRepos;

namespace Repo
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
            var entity = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
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
    }   
}
