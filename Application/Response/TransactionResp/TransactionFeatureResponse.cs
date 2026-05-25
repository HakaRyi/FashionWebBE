using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Response.TransactionResp
{
    public class TransactionFeatureResponse
    {
    }

    public class FeatureIntelligenceResponse
    {
        public KpiDashboard Kpis { get; set; } = new();
        public List<FeatureRevenueDto> FeatureRevenue { get; set; } = new();
        public List<CreditVelocityDto> CreditVelocity { get; set; } = new();
        public List<LiveTransactionDto> LiveTransactions { get; set; } = new();
    }

    public class KpiDashboard
    {
        public decimal GrossFeatureRevenue { get; set; }
        public decimal InfrastructureCost { get; set; }
        public int ActiveFeatureUsers { get; set; }
        public decimal NetProfitMargin { get; set; }
        public Dictionary<string, string> Trends { get; set; } = new();
    }

    public class FeatureRevenueDto
    {
        public string Feature { get; set; } = null!;
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public int Users { get; set; }
        public decimal Growth { get; set; }
        public decimal Roi => Cost > 0 ? Math.Round(Revenue / Cost, 1) : 0;
    }

    public class CreditVelocityDto
    {
        public string Time { get; set; } = null!;
        public int ApiCalls { get; set; }
        public decimal Spend { get; set; }
    }

    public class LiveTransactionDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = null!;
        public string Item { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
