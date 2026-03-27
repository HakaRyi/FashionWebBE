using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public string? ThumbnailUrl { get; set; }

        // Cơ cấu giải thưởng (Rút gọn)
        public decimal TotalPrizePool { get; set; }
        public List<PrizeBriefDto> Prizes { get; set; } = new();

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
        public DateTime? EndTime { get; set; }
        public string? Status { get; set; }

        // Thông tin người tạo
        public int CreatorId { get; set; }
        public string? CreatorName { get; set; }

        // Danh sách giải thưởng (Đã lọc bỏ thông tin EscrowSession nhạy cảm)
        public List<PrizeDtoV1> Prizes { get; set; } = new();

        // Danh sách chuyên gia/giám khảo
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
}
