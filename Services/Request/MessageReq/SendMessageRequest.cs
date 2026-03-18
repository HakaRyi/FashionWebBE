using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.DTO.Request
{
    public class SendMessageRequest
    {
        public string? content {  get; set; }
        public List<string>? photoUrls { get; set; }
        public int? replyToId { get; set; }
        public bool isRecalled { get; set; } = false;
    }
}
