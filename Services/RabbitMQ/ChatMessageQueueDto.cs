using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.RabbitMQ
{
    public class ChatMessageQueueDto
    {
        public int GroupId { get; set; }
        public int SenderId { get; set; }
        public string Content { get; set; }
        public List<string> ImageUrls { get; set; }
        public int? ReplyToId { get; set; }
    }
}
