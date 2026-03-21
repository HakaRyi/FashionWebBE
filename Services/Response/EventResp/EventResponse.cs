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

    public class PrizeDto
    {
        public string Label { get; set; } = null!;
        public double Amount { get; set; }
    }

    public class CreateEventDto
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double ExpertWeight { get; set; }
        public List<PrizeDto> Prizes { get; set; } = new();
        public List<string> Hashtags { get; set; } = new();
    }

    public class DepositDto
    {
        public int AccountId { get; set; }
        public double Amount { get; set; }
        public string TransactionCode { get; set; } = null!;
    }

    public class EventListDto
    {
        public int EventId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? Status { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int ParticipantCount { get; set; } // Số lượng bài post tham gia
        public string? CreatorName { get; set; }
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
        public string? ExpertiseField { get; set; } // Lấy từ ExpertProfile
    }
}
