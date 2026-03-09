using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Entities
{
    public partial class EventWinner
    {
        public int EventWinnerId { get; set; }
        public int AccountId { get; set; }
        public int PrizeEventId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Status { get; set; }
        public virtual Account Account { get; set; }
        public virtual PrizeEvent PrizeEvent { get; set; }
    }
}
