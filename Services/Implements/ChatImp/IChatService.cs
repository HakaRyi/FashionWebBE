using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Entities;
using Service.DTO.Request;
using Services.Response.MessageResp;
using Services.Response.MessReactResp;

namespace Services.Implements.ChatImp
{
    public interface IChatService
    {
        Task SendMessage(int groupId,SendMessageRequest request);
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

    }
}
