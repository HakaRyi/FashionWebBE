namespace Application.Request.NotificationReq
{
    public class SendNotificationRequest
    {
        public int SenderId { get; set; }
        public int? TargetUserId { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? Type { get; set; }
        public string? RelatedId { get; set; }
    }
}