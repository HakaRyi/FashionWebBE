using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Entities;

namespace Repositories.Repos.ChatRepos
{
    public interface IChatRepository
    {
        Task<Group> GetGroupById(int id);
        Task<Account> GetAccountById(int id);
        Task<Message> GetMessageById(int id);
        Task AddMessage(Message message);
        Task DeleteMessage(Message message);
        Task EditMessage(Message message);
        Task<List<Message>> GetHistoryMessages(int groupId);
        Task AddOrUpdateReaction(MessReaction reaction);
        Task<List<MessReaction>> GetAllReactionByMessageiD(int messageId);
    }
}
