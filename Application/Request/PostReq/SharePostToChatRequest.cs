namespace Application.Request.PostReq
{
    public class SharePostToChatRequest
    {
        public int PostId { get; set; }
        public List<int> ReceiverAccountIds { get; set; } = new();
        public string? Caption { get; set; }
    }
}