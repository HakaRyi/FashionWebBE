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
}
