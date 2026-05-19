using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Response.OrderResp
{
    public class OrderAdminDetailResponse
    {
        public string Id { get; set; } = null!; // Hiển thị OrderCode hoặc ORD-Id
        public string TransactionId { get; set; } = null!;
        public string PlacedAt { get; set; } = null!;
        public int CurrentStatus { get; set; } // Số bước (0, 1, 2, 3) cho Stepper

        public BuyerDto Buyer { get; set; } = new BuyerDto();
        public SellerDto Seller { get; set; } = new SellerDto();
        public PaymentDto Payment { get; set; } = new PaymentDto();
        public LedgerDto Ledger { get; set; } = new LedgerDto();
        public FinancialAuditDto FinancialAudit { get; set; } = new FinancialAuditDto();
        public List<ItemDto> Items { get; set; } = new List<ItemDto>();
        public List<HistoryDto> History { get; set; } = new List<HistoryDto>();
    }

    public class BuyerDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Address { get; set; } = null!;
    }

    public class SellerDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Warehouse { get; set; } = null!;
    }

    public class PaymentDto
    {
        public string Method { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string Gateway { get; set; } = null!;
    }

    public class LedgerDto
    {
        public decimal Subtotal { get; set; }
        public decimal Shipping { get; set; }
        public decimal Tax { get; set; }
        public decimal PlatformFee { get; set; }
        public decimal SellerNet { get; set; }
        public decimal Total { get; set; }
    }

    public class FinancialAuditDto
    {
        public List<WalletLogDto> SettlementLogs { get; set; } = new List<WalletLogDto>();
    }

    public class WalletLogDto
    {
        public string ActorType { get; set; } = null!; // "Buyer", "Seller", "Platform"
        public string ActorName { get; set; } = null!; // Tên hiển thị (Tên khách, tên shop, hoặc "System")
        public string ActionType { get; set; } = null!; // "Debit" (Trừ) hoặc "Credit" (Cộng)
        public string Description { get; set; } = null!; // "Thanh toán đơn hàng", "Hoàn tiền hủy đơn",...
        public decimal Amount { get; set; }           // Số tiền biến động (Luôn dương, hướng đi do ActionType quyết định)
        public decimal WalletBefore { get; set; }
        public decimal WalletAfter { get; set; }
        public string Timestamp { get; set; } = null!;  // Thời gian xảy ra giao dịch cụ thể
    }

    public class UserAuditDto
    {
        public decimal WalletBefore { get; set; }
        public decimal WalletAfter { get; set; }
    }

    public class PlatformAuditDto
    {
        public decimal WalletBefore { get; set; }
        public decimal WalletAfter { get; set; }
    }

    public class ItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public int Qty { get; set; }
        public string Sku { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string Condition { get; set; } = null!;
        public string? ImageUrl { get; set; }
    }

    public class HistoryDto
    {
        public string Status { get; set; } = null!;
        public string Time { get; set; } = null!;
    }
}
