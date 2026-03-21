using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Response.WalletResp
{
    public class WalletResponse
    {
        public int WalletId { get; set; }
        public decimal Balance { get; set; }
        public string? Currency { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class WalletDashboardResponse
    {
        public List<StatCardDto> Stats { get; set; } = new();
        public List<TransactionDto> Transactions { get; set; } = new();
    }

    public class StatCardDto
    {
        public string Label { get; set; }    // "Số dư hiện tại"
        public string Value { get; set; }    // "1,500"
        public string Sub { get; set; }      // "VNĐ" hoặc "Coins"
        public string Icon { get; set; }     // Tên icon để FE map (ví dụ: "Wallet")
    }

    public class TransactionDto
    {
        public string Id { get; set; }       // "GD12345"
        public string Detail { get; set; }   // "Nạp tiền qua VNPay"
        public string Date { get; set; }     // "20/03/2026"
        public decimal Amount { get; set; }  // 500000
        public string Type { get; set; }     // "deposit" hoặc "expense"
        public string Status { get; set; }   // "Success", "Pending"
    }

}
