using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Service.DTO.Request;
using Services.Response.MessageResp;

namespace Services.Implements.ChatImp
{
    public interface IChatService
    {
        Task SendMessage(int groupId,SendMessageRequest request);
        Task UpdateMessage(int messageId, EditMessageRequest request);
        Task DeleteMessage(int messageId);
        Task<List<MessageResponse>> GetHistoryMessage(int groupId);
        Task<MessageResponse> GetMessageById(int messageId);
    }
}
