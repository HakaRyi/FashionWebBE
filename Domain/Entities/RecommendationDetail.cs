using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class RecommendationDetail
    {
        public int Id { get; set; }
        public int RecommendationHistoryId { get; set; }
        public int ItemId { get; set; }
        public virtual RecommendationHistory RecommendationHistory { get; set; } = null!;
        public virtual Item Item { get; set; } = null!;
    }
}
