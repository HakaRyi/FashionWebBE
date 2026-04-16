namespace Application.Response.PostResp
{
    public class PostResponse
    {
        public int PostId { get; set; }
        public int AccountId { get; set; }
        public string? UserName { get; set; }
        public string? AvatarUrl { get; set; }
        public int? EventId { get; set; }
        public string? EventName { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public List<string>? ImageUrls { get; set; }
        public bool? IsExpertPost { get; set; }
        public bool IsLikedByExpert { get; set; }
        public string? Status { get; set; }
        public double? Score { get; set; }
        public string? Reason { get; set; }
        public int? LikeCount { get; set; }
        public int? CommentCount { get; set; }
        public int? ShareCount { get; set; }
        public bool IsLiked { get; set; }
        public bool IsSaved { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<CriterionRatingResponse> CriterionRatings { get; set; } = new();
    }

    public class CriterionRatingResponse
    {
        public int EventCriterionId { get; set; }
        public double Score { get; set; }
    }

    public class PostReviewDto
    {
        public int PostId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public string? AuthorName { get; set; }

        public double? CurrentScore { get; set; }
        public string? MyReason { get; set; }
        public bool IsGraded { get; set; }

        public int LikeCount { get; set; }
        public int ShareCount { get; set; }

        public DateTime SubmittedAt { get; set; }
    }
}