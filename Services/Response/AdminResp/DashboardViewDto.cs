using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Response.AdminResp
{
    public class DashboardViewDto
    {
        public OverviewDto Overview { get; set; }
        public List<ChartPointDto> RevenueChart { get; set; }
        public List<ChartPointDto> UserChart { get; set; }
        public List<ChartPointDto> ExpertChart { get; set; }
        public List<ChartPointDto> PostChart { get; set; }
    }

    public class OverviewDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalUsers { get; set; }
        public int TotalExperts { get; set; }
        public int TotalPosts { get; set; }
    }

    public class ChartPointDto
    {
        public string Name { get; set; }
        public decimal Value { get; set; }
    }
}
