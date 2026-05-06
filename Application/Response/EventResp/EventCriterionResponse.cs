using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Response.EventResp
{
    public class EventCriterionResponse
    {
        public int EventCriterionId { get; set; }
        public int EventId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public double WeightPercentage { get; set; }
    }
}
