using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class RecommendationHistory
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int? ReferenceItemId { get; set; }
        public string? Prompt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public virtual Account Account { get; set; } = null!;
        public virtual Item? ReferenceItem { get; set; }
        public virtual ICollection<RecommendationDetail> RecommendedItems { get; set; } = new List<RecommendationDetail>();
    }
}
