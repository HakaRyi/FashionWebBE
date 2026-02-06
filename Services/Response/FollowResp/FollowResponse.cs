using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Response.FollowResp
{
    public class FollowResponse
    {
        public int UserId { get; set; }
        public int FollowerId { get; set; }
        public string FollowerAvatar { get; set; } = null!;
        public string FollowerName { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
    }
}
