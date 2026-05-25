using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Response.TransactionResp
{
    public class WhaleDashboardResponse
    {
    }

    public class WhaleDashboardDto
    {
        public string Id { get; set; } = null!;          // Map với AccountId hoặc WalletId (Ví dụ: "W-1")
        public string Name { get; set; } = null!;        // Account.FullName hoặc Account.Email
        public decimal TotalSpent { get; set; }          // Tổng tiền đã tiêu (LTV)
        public decimal AvgTicket { get; set; }           // Trung bình một đơn hàng/giao dịch chi tiêu
        public int RetentionScore { get; set; }          // Điểm giữ chân (tự tính dựa trên tần suất mua)
        public List<WhaleTrendDto> TrendData { get; set; } = new();
    }

    public class WhaleTrendDto
    {
        public string Name { get; set; } = null!;        // Thứ (Mon, Tue...) hoặc Tháng (Jan, Feb...)
        public decimal Personal { get; set; }            // Chi tiêu của chính whale đó
        public decimal TotalAvg { get; set; }            // Chi tiêu trung bình của toàn sàn/hệ thống
    }

    // --- DTOs cho Lịch sử giao dịch (History Panel) ---
    public class WhaleHistoryDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public decimal TotalSpent { get; set; }
        public List<TransactionDistributionDto> DistributionData { get; set; } = new();
        public List<WhaleTransactionDto> Transactions { get; set; } = new();
    }

    public class TransactionDistributionDto
    {
        public string Name { get; set; } = null!;        // ReferenceType (Ví dụ: OrderPayment, Upgrade...)
        public decimal Value { get; set; }               // Tổng tiền của nhóm này
    }

    public class WhaleTransactionDto
    {
        public string Id { get; set; } = null!;          // Mã giao dịch (TransactionCode hoặc ID)
        public string Date { get; set; } = null!;        // Định dạng chuỗi yyyy-MM-dd HH:mm
        public string Feature { get; set; } = null!;     // Tên tính năng (ReferenceType hoặc Description)
        public decimal Amount { get; set; }
        public string Status { get; set; } = null!;
        public string Method { get; set; } = null!;      // Payment Method hoặc mặc định Wallet
    }
}
