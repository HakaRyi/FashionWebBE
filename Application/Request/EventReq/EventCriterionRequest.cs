using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Request.EventReq
{
    public class EventCriterionRequest
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public double WeightPercentage { get; set; }
    }
}
