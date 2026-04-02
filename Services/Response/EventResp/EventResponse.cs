namespace Services.Response.EventResp
{
    public class EventResponse
    {
    }

    public class EventListDto
    {
        public int EventId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? Status { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? SubmissionDeadline { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Thông tin định lượng
        public int ParticipantCount { get; set; }
        public string? CreatorName { get; set; }
        public string? CreatorAvatarUrl { get; set; }
        public string? ThumbnailUrl { get; set; }

        // Cơ cấu giải thưởng (Rút gọn)
        public decimal TotalPrizePool { get; set; }
        public List<PrizeBriefDto> Prizes { get; set; } = new();


        public bool IsAutoStart { get; set; }
        public bool IsJoined { get; set; }
        public string? MyExpertStatus { get; set; }
    }

    public class PrizeBriefDto
    {
        public int Ranked { get; set; }
        public decimal RewardAmount { get; set; }
    }

    // DTO chi tiết sự kiện
    public class EventDetailDto
    {
        public int EventId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public double ExpertWeight { get; set; }
        public double UserWeight { get; set; }
        public decimal AppliedFee { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? SubmissionDeadline { get; set; }
        public string? ThumbnailUrl { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Status { get; set; }
        public decimal TotalPrizePool { get; set; }
        public int ParticipantCount { get; set; }

        // Thông tin người tạo
        public int CreatorId { get; set; }
        public string? CreatorName { get; set; }

        // --- CÁC TRƯỜNG MỚI ĐỂ FE XỬ LÝ NÚT BẤM VÀ TIẾN ĐỘ ---
        public bool IsJoined { get; set; }
        public bool IsAutoStart { get; set; }
        public int MinExpertsToStart { get; set; }
        public int AcceptedExpertsCount { get; set; }
        public bool CanManualStart { get; set; }
        public bool CanFinalize { get; set; }

        public List<PrizeDtoV1> Prizes { get; set; } = new();
        public List<ExpertInEventDto> Experts { get; set; } = new();
    }

    public class PrizeDtoV1
    {
        public int PrizeEventId { get; set; }
        public int Ranked { get; set; }
        public decimal RewardAmount { get; set; }
        public string? Status { get; set; }
    }

    public class ExpertInEventDto
    {
        public int ExpertId { get; set; }
        public string? FullName { get; set; }
        public string? ExpertiseField { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Status { get; set; }
    }

    public class EventAdminListDto
    {
        public int EventId { get; set; }
        public string Title { get; set; } = null!;
        public string? Status { get; set; }
        public string? Note { get; set; }

        // Thông tin người tạo
        public int CreatorId { get; set; }
        public string? CreatorName { get; set; }
        public string? CreatorEmail { get; set; }

        // Thông tin tài chính & Cấu hình
        public decimal AppliedFee { get; set; }
        public decimal TotalPrizePool { get; set; }
        public int MinExperts { get; set; }
        public int CurrentAcceptedExperts { get; set; }

        // Thời gian
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Chỉ số tương tác
        public int ParticipantCount { get; set; }
        public string? ThumbnailUrl { get; set; }
    }
    //BXH
    public class EventLeaderboardDto
    {
        public int Rank { get; set; }
        public int AccountId { get; set; }
        public string UserName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public double FinalScore { get; set; }
        public int PostId { get; set; }
        public decimal? RewardAmount { get; set; }
    }

    public class MyEventResultDetailDto
    {
        public int Rank { get; set; }
        public double MyScore { get; set; }
        public string? MyPostImageUrl { get; set; }
        public List<ExpertReviewDto> ExpertReviews { get; set; } = new();
        public List<VoterDto> Voters { get; set; } = new();
    }

    public class ExpertReviewDto
    {
        public string ExpertName { get; set; } = null!;
        public string? ExpertAvatar { get; set; }
        public double Score { get; set; }
        public string? Reason { get; set; }
    }

    public class VoterDto
    {
        public string UserName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public DateTime VotedAt { get; set; }
    }
}
