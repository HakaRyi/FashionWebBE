using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Entities;

namespace Repositories.Repos.ChatRepos
{
    public interface IPinMessageRepository
    {
        Task<PinnedMessage> GetPinnedMessageAsync(int pinnedMsgId);
        Task<List<PinnedMessage>> GetPinnedMessagesByGroupIdAsync(int groupId);
        Task AddPinnedMessageAsync(PinnedMessage pinnedMessage);
        Task RemovePinnedMessageAsync(PinnedMessage pinnedMessage);
    }
}
