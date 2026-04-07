using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public partial class ExpertCriterionRating
    {
        public int ExpertCriterionRatingId { get; set; }

        public int ExpertRatingId { get; set; }

        public int EventCriterionId { get; set; }

        public double Score { get; set; }

        public virtual ExpertRating ExpertRating { get; set; } = null!;
        public virtual EventCriterion EventCriterion { get; set; } = null!;
    }
}
