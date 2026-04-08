using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public partial class EventCriterion
    {
        public int EventCriterionId { get; set; }

        public int EventId { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public double WeightPercentage { get; set; }

        public virtual Event Event { get; set; } = null!;

        public virtual ICollection<ExpertCriterionRating> CriterionRatings { get; set; } = new List<ExpertCriterionRating>();
    }
}
