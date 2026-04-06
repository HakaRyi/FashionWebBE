using Domain.Entities;

namespace Domain.Interfaces

{
    public interface IPinMessageRepository
    {
        Task<PinnedMessage> GetPinnedMessageAsync(int pinnedMsgId);
        Task<List<PinnedMessage>> GetPinnedMessagesByGroupIdAsync(int groupId);
        Task AddPinnedMessageAsync(PinnedMessage pinnedMessage);
        Task RemovePinnedMessageAsync(PinnedMessage pinnedMessage);
    }
}
