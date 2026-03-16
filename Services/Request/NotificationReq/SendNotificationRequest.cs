using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Request.NotificationReq
{
    public class SendNotificationRequest
    {
        public int SenderId { get; set; }
        public int? TargetUserId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string Type { get; set; } = null!;
    }
}
