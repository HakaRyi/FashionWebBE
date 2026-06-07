using System;
using System.Collections.Generic;

namespace Application.Response.OrderResp
{
    public class OrderAdminDetailResponse
    {
        public int OrderId { get; set; }
        public string Id { get; set; } = null!;
        public string TransactionId { get; set; } = null!;
        public string PlacedAt { get; set; } = null!;
        public string StatusKey { get; set; } = null!;
        public int CurrentStatusStep { get; set; }

        public bool HasRefundRequest { get; set; }
        public string? RefundRequestStatus { get; set; }
        public string? RefundReason { get; set; }
        public string? RefundAdminNote { get; set; }
        public DateTime? RefundRequestedAt { get; set; }
        public DateTime? RefundProcessedAt { get; set; }

        public BuyerDto Buyer { get; set; } = new();
        public SellerDto Seller { get; set; } = new();
        public PaymentDto Payment { get; set; } = new();
        public LedgerDto Ledger { get; set; } = new();
        public FinancialAuditDto FinancialAudit { get; set; } = new();
        public List<ItemDto> Items { get; set; } = new();
        public List<HistoryDto> History { get; set; } = new();
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
        public List<WalletLogDto> SettlementLogs { get; set; } = new();
    }

    public class WalletLogDto
    {
        public string ActorType { get; set; } = null!; // "Buyer", "Seller", "Platform"
        public string ActorName { get; set; } = null!; // Tên hiển thị
        public string ActionType { get; set; } = null!; // "Debit" hoặc "Credit"
        public string Description { get; set; } = null!;
        public decimal Amount { get; set; }
        public decimal WalletBefore { get; set; }
        public decimal WalletAfter { get; set; }
        public string Timestamp { get; set; } = null!;
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
        public string StatusKey { get; set; } = null!;
        public string StatusDisplay { get; set; } = null!;
        public DateTime ChangedAt { get; set; }
        public string DateOnly { get; set; } = null!;
        public string TimeOnly { get; set; } = null!;
        public string? Note { get; set; }
    }
}