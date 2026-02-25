using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Response.AccountRep
{
    public class FashionExpertResponse
    {
        public string Avatar { get; set; } 
        public int AccountId { get; set; }
        public int ExpertProfileId { get; set; }
        public string FullName { get; set; } = null!;
        public bool? Verified { get; set; } 
        public string? ExpertiseField { get; set; }
        public double? Rating { get; set; }
        public int FollowerCount { get; set; }
        public int FollowingCount { get; set; }
        public int PostCount { get; set; }
        public string? Description { get; set; }
    }
}
