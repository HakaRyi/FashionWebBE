using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Services.Request.NotificationReq;
using Services.Response.EventResp;

namespace Services.Response.AdminResp
{
    public class AdminEventResponse
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
    public class PagedAdminEventResponse
    {
        public List<AdminEventResponse> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
