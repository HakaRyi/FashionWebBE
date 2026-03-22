namespace Services.Response.PostResp
{
    public class PostResponse
    {
        public int PostId { get; set; }
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

    public class PostReviewDto
    {
        public int PostId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; } // Nội dung bài viết để Expert đọc
        public string? ImageUrl { get; set; } // Ảnh đại diện của bài nộp
        public string? AuthorName { get; set; }

        // --- Logic Chấm điểm ---
        public double? CurrentScore { get; set; } // Điểm mà Expert này đã chấm (nullable)
        public string? MyReason { get; set; }     // Lý do Expert đã ghi chú khi chấm
        public bool IsGraded { get; set; }       // Trạng thái đã chấm hay chưa

        // --- Thông số cộng đồng (để tham khảo khi chấm) ---
        public int LikeCount { get; set; }
        public int ShareCount { get; set; }

        public DateTime SubmittedAt { get; set; }
    }
}
