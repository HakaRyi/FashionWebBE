namespace Application.Response.DashboardResp
{
    public class EventAnalyticsRawResponse
    {
        public int ExpertId { get; set; }
        public List<EventRawDto> Events { get; set; } = new List<EventRawDto>();
    }

    public class EventRawDto
    {
        public int EventId { get; set; }
        public string Title { get; set; }
        public decimal AppliedFee { get; set; }
        public decimal EntryFee { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
        public List<PostRawDto> Posts { get; set; }

        public int TotalPosts => Posts?.Count ?? 0;
        public decimal TotalEntryRevenue => TotalPosts * EntryFee;
    }

    public class PostRawDto
    {
        public int PostId { get; set; }
        public int LikeCount { get; set; }
        public int ShareCount { get; set; }
        public int CommentCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRatedByExpert { get; set; }
    }
}
