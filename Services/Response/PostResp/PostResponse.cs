namespace Services.Response.PostResp
{
    public class PostResponse
    {
        public int PostId { get; set; }
        public int AccountId { get; set; }
        public string UserName { get; set; }
        public string AvatarUrl { get; set; }
        public int? EventId { get; set; }
        public string? EventName { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public List<string>? ImageUrls { get; set; }
        public bool? IsExpertPost { get; set; }
        public string? Status { get; set; }
        public double? Score { get; set; }
        public int? LikeCount { get; set; }
        public int? ShareCount { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
