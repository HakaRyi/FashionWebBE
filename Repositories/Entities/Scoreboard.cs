using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Entities
{
    public partial class Scoreboard
    {
        public int ScoreboardId { get; set; }
        public int PostId { get; set; }
        public double Score { get; set; }
        public int Like { get; set; }
        public int Share { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
        public virtual Post Post { get; set; }
    }
}
