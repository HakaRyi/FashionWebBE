using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Entities
{
    public partial class Follow
    {
        public int UserId { get; set; } 

        public int FollowerId { get; set; }

        public DateTime? CreatedAt { get; set; }

        // Navigation properties
        public virtual Account User { get; set; } = null!;
        public virtual Account Follower { get; set; } = null!;
    }
}
