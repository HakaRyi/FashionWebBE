using Services.Request.PrizeReq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Request.EventReq
{
    public class EventRequest
    {
    }

    public class CreateEventRequest
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double ExpertWeight { get; set; }
        public double UserWeight { get; set; }
        public double PointPerLike { get; set; }
        public double PointPerShare { get; set; }
        public List<PrizeRequest> Prizes { get; set; } = new();
        public List<int>? InvitedExpertIds { get; set; }
    }
}
