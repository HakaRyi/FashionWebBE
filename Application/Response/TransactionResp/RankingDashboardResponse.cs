using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Response.TransactionResp
{
    public class RankingDashboardResponse
    {
        public int ActiveNodes { get; set; }
        public double MarketReach { get; set; }
        public string LeaderboardAlpha { get; set; } = null!;
        public decimal AvgTicketSize { get; set; }
        public List<int> GlobalTrend { get; set; } = new();
        public List<string> ChartLabels { get; set; } = new();
        public List<ShopRankingDto> Shops { get; set; } = new();
    }

    public class ShopRankingDto
    {
        public int Id { get; set; } // Map với WalletId hoặc ShopId
        public string Name { get; set; } = null!; // Map với UserName
        public decimal Share { get; set; } // Thị phần (%)
        public decimal Revenue { get; set; } // Tổng doanh thu thành công
        public decimal Growth { get; set; } // % Tăng trưởng so với tháng trước
        public string Status { get; set; } = null!; // Elite, Stable, Rising, Under Review
        public int Orders { get; set; } // Tổng số đơn hàng
        public List<int> MonthlyTrend { get; set; } = new(); // Xu hướng doanh thu 6 tháng gần nhất (Index từ 0 - 100)
    }
}
