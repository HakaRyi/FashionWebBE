using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.RabbitMQ
{
    public class PostImageMessage
    {
        public int PostId { get; set; }
        public List<string> ImageUrls { get; set; } = new();
    }
}
