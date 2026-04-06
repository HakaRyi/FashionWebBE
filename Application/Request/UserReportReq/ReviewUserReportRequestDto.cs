namespace Application.Request.UserReportReq
{
    public class ReviewUserReportRequestDto
    {
        public string NewStatus { get; set; } = string.Empty;
        public string? AdminNote { get; set; }

        // true => khi report đúng thì ẩn bài
        public bool HidePostWhenResolved { get; set; } = true;

        // optional:
        // null => không đổi Post.Status
        // ví dụ: "Rejected" hoặc "Banned"
        public string? PostStatusToApply { get; set; }
    }
}