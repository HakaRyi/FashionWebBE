using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Entities
{
    public partial class ExpertRating
    {
        public int ExpertRatingId { get; set; }

        public int PostId { get; set; }

        public int ExpertId { get; set; }

        public double Score { get; set; }

        public string? Reason { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public virtual Post Post { get; set; } = null!;

        public virtual Account Expert { get; set; } = null!;
    }
}
