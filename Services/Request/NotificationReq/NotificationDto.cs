using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Request.NotificationReq
{
    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? Type { get; set; }
        public string? SenderName { get; set; }
        public string? SenderAvatar { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class PagedNotificationResponse
    {
        public List<NotificationDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
