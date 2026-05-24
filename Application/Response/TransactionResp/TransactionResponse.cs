namespace Application.Response.TransactionResp
{
    public class TransactionResponse
    {
        public int TransactionId { get; set; }
        public int WalletId { get; set; }
        public string? UserName { get; set; }
        public int? PaymentId { get; set; }
        public string TransactionCode { get; set; } = null!;
        public decimal Amount { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public string? Type { get; set; }
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
        public string? EventName { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Status { get; set; }
    }

    public class TransactionResponseV2
    {
        // --- Thông tin giao dịch cơ bản ---
        public int TransactionId { get; set; }
        public string TransactionCode { get; set; } = null!;
        public decimal Amount { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public string? Type { get; set; } // Credit, Debit...
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Status { get; set; }

        // --- Đối tượng Ví & Người dùng ---
        public int WalletId { get; set; }
        public string? UserName { get; set; } // Lấy từ Wallet.Account.FullName hoặc Email
        public bool IsSystemWallet => WalletId == 1; // Chỉ dấu nhanh cho Frontend đổi màu hàng (Hệ thống vs User)

        //Đối tượng đối ứng giao dịch
        public int? CounterpartyUserId { get; set; }  // ID của người đối ứng (null nếu là nạp tiền hệ thống)
        public string? CounterpartyName { get; set; }

        // --- Quy chiếu Hệ thống (Nguồn gốc dòng tiền) ---
        public string? ReferenceType { get; set; } // OrderPayment, OrderRefund, Event, TryOn...
        public int? ReferenceId { get; set; }
        public int? PaymentId { get; set; }

        // --- THÔNG TIN BỔ SUNG: Đối soát Quỹ trung gian Escrow ---
        // Trường hợp ReferenceType là 'Event' hoặc 'Order', Admin cần biết thông tin phiên ký quỹ liên quan
        public EscrowBriefResponse? EscrowDetail { get; set; }

        public SystemWalletSnapshotResponse? SystemWalletSnapshot { get; set; }
    }

    public class SystemWalletSnapshotResponse
    {
        public decimal PlatformAmountChanged { get; set; } // Số tiền Admin thực thu (+) hoặc thực chi (-)
        public decimal PlatformBalanceAfter { get; set; }   // Số dư ví Admin ngay sau giao dịch này (0 hoặc N/A nếu là data cũ giả lập)
        public string DataMode { get; set; } = "Real";       // "Real" (Dữ liệu thật trong DB) hoặc "Fallback" (Chữa cháy lập luận)
    }

    public class EscrowBriefResponse
    {
        public int EscrowSessionId { get; set; }
        public string Status { get; set; } = null!;
        public decimal OriginalAmount { get; set; }   // Số tiền người mua/người tham gia bỏ ra
        public decimal SystemServiceFee { get; set; } // Phí hệ thống đút túi (Doanh thu của Admin)
        public decimal FinalPayoutAmount { get; set; } // Số tiền thực tế sẽ giải ngân cho bên thụ hưởng
        public string EscrowAction { get; set; } = "None"; // "Held" (Giữ tiền), "Released" (Giải ngân), "Refunded" (Hoàn tiền)
        public decimal EscrowAmountChanged { get; set; }
        public decimal EscrowBefore { get; set; }      // Số dư quỹ trung gian TRƯỚC giao dịch
        public decimal EscrowAfter { get; set; }
        public string? LinkedTargetName { get; set; }  // Tên Sự kiện hoặc Mã đơn hàng gắn liền
        public string? SenderName { get; set; }
        public string? ReceiverName { get; set; }
    }
}