using Application.Request.MessageReq;
using Application.Response.MessageResp;
using Application.Response.MessReactResp;

namespace Application.Services.ChatImp
{
    public interface IChatService
    {
        Task SendMessage(int groupId, SendMessageRequest request);
        Task UpdateMessage(int messageId, EditMessageRequest request);
        Task DeleteMessage(int messageId);
        Task<List<MessageResponse>> GetHistoryMessage(int groupId);
        Task<MessageResponse> GetMessageById(int messageId);
        Task RecallMessage(int messageId);
        Task AddReaction(int messageId, string type);
        Task PinMessage(int messageId, int groupId);
        Task UnPinMessage(int pinMsg);
        Task<List<PinMessageResponse>> GetPinnedMessagesByGroupId(int groupId);
        Task<List<MessReactResponse>> GetReactorByMessId(int messId);
        Task<int> SendConsultationRequest(int itemId);

    }
}
