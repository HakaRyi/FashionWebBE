using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Entities
{
    public partial class PrizeEvent
    {
        public int PrizeEventId { get; set; }
        public int EventId { get; set; }
        public string Ranked { get; set; }
        public int RewardCoin { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Status { get; set; }
        public virtual Event Event { get; set; }
        public virtual EventWinner EventWinner { get; set; }
    }
}
