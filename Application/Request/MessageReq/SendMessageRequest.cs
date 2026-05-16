using Microsoft.AspNetCore.Http;

namespace Application.Request.MessageReq
{
    public class SendMessageRequest
    {
        public string? content { get; set; }
        public List<IFormFile>? photo { get; set; }
        public int? replyToId { get; set; }
        public bool isRecalled { get; set; } = false;
    }
}
