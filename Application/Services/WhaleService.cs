using Application.Interfaces;
using Application.Response.TransactionResp;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class WhaleService : IWhaleService
    {
        private readonly ITransactionRepository _transactionRepo;

        public WhaleService(ITransactionRepository transactionRepo)
        {
            _transactionRepo = transactionRepo;
        }

        public async Task<List<WhaleDashboardDto>> GetTopWhalesAsync(DateTime fromDate, DateTime toDate, string? viewMode, string? searchQuery)
        {
            var endOfToDate = toDate.Date.AddDays(1).AddTicks(-1);
            var allTransactions = await _transactionRepo.GetAllTransactionsAsync(fromDate, endOfToDate);

            // Lấy tất cả giao dịch hợp lệ của TOÀN SÀN trước (Dùng để tính Average chuẩn không bị ảnh hưởng bởi thanh Search)
            var validPaymentTxs = allTransactions
                .Where(t => t.Status == "Success" && t.Wallet?.Account?.UserName != "admin")
                .ToList();

            // SỬ CHÍ MẠNG BUG 1 & 3: Tính toán trước Trung bình toàn sàn theo từng Slot ra một Dictionary độc lập
            string intervalType = !string.IsNullOrEmpty(viewMode)
                ? viewMode.ToLower()
                : ((toDate - fromDate).TotalDays > 60 ? "monthly" : "daily");

            var timelineSlots = GenerateTimelineSlots(fromDate, toDate, intervalType);
            var platformAvgPerSlotDict = new Dictionary<string, decimal>();

            foreach (var slot in timelineSlots)
            {
                var platformTxInSlot = validPaymentTxs.Where(t => IsTransactionInSlot(t.CreatedAt, slot.Key, intervalType)).ToList();
                var slotActiveWallets = platformTxInSlot.GroupBy(t => t.WalletId)
                    .Select(g => {
                        decimal d = 0; decimal r = 0;
                        foreach (var t in g)
                        {
                            var desc = (t.Description ?? "").ToLower();
                            if (t.Type == "Debit" || t.Type == "System_Fee_Payment" || desc.Contains("pay for") || desc.Contains("paid entry fee")) d += Math.Abs(t.Amount);
                            else if (t.Type == "Event_Refund" || t.ReferenceType == "OrderRefund" || desc.Contains("refund")) r += Math.Abs(t.Amount);
                        }
                        return new { NetSpent = d - r };
                    }).Where(x => x.NetSpent > 0).ToList();

                decimal totalAvg = slotActiveWallets.Any() ? slotActiveWallets.Average(w => w.NetSpent) : 0;
                platformAvgPerSlotDict[slot.Key] = Math.Round(totalAvg, 2);
            }

            // Bây giờ mới áp dụng searchQuery cho danh sách hiển thị Whale (Không lo lệch đường trung bình sàn nữa)
            var filteredWhaleTxs = validPaymentTxs;
            if (!string.IsNullOrEmpty(searchQuery))
            {
                filteredWhaleTxs = validPaymentTxs
                    .Where(t => t.Wallet?.Account?.UserName != null &&
                                t.Wallet.Account.UserName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Gom nhóm khách hàng VIP
            var whaleGroups = filteredWhaleTxs
                .GroupBy(t => new { t.WalletId, Name = t.Wallet?.Account?.UserName ?? "Anonymous customer" })
                .Select(g =>
                {
                    decimal totalDebit = 0; decimal totalRefund = 0; int totalOrders = 0;
                    foreach (var t in g)
                    {
                        var desc = (t.Description ?? "").ToLower();
                        if (t.Type == "Debit" || t.Type == "System_Fee_Payment" || desc.Contains("pay for") || desc.Contains("paid entry fee"))
                        {
                            totalDebit += Math.Abs(t.Amount);
                            totalOrders++;
                        }
                        else if (t.Type == "Event_Refund" || t.ReferenceType == "OrderRefund" || desc.Contains("refund"))
                        {
                            totalRefund += Math.Abs(t.Amount);
                        }
                    }

                    var realTotalSpent = totalDebit - totalRefund;
                    return new
                    {
                        g.Key.WalletId,
                        g.Key.Name,
                        TotalSpent = realTotalSpent > 0 ? realTotalSpent : 0,
                        AvgTicket = totalOrders > 0 ? (totalDebit / totalOrders) : 0,
                        TxCount = totalOrders
                    };
                })
                .Where(w => w.TotalSpent > 0)
                .OrderByDescending(g => g.TotalSpent)
                .ToList();

            var result = new List<WhaleDashboardDto>();

            // Khớp nối dữ liệu Trend (Chạy mượt mà, siêu tốc)
            foreach (var whale in whaleGroups)
            {
                int retentionScore = Math.Min(100, whale.TxCount * 5 + 45);
                var trendData = new List<WhaleTrendDto>();
                var personalTx = filteredWhaleTxs.Where(t => t.WalletId == whale.WalletId).ToList();

                foreach (var slot in timelineSlots)
                {
                    var slotPersonalTx = personalTx.Where(t => IsTransactionInSlot(t.CreatedAt, slot.Key, intervalType)).ToList();

                    decimal pDebit = 0; decimal pRefund = 0;
                    foreach (var t in slotPersonalTx)
                    {
                        var desc = (t.Description ?? "").ToLower();
                        if (t.Type == "Debit" || t.Type == "System_Fee_Payment" || desc.Contains("pay for") || desc.Contains("paid entry fee")) pDebit += Math.Abs(t.Amount);
                        else if (t.Type == "Event_Refund" || t.ReferenceType == "OrderRefund" || desc.Contains("refund")) pRefund += Math.Abs(t.Amount);
                    }

                    var personalSpent = pDebit - pRefund;

                    trendData.Add(new WhaleTrendDto
                    {
                        Name = slot.Value,
                        Personal = personalSpent > 0 ? personalSpent : 0,
                        TotalAvg = platformAvgPerSlotDict[slot.Key] // Lấy trực tiếp từ Dictionary, mất O(1) để lấy dữ liệu!
                    });
                }

                result.Add(new WhaleDashboardDto
                {
                    Id = $"W-{whale.WalletId}",
                    Name = whale.Name,
                    TotalSpent = whale.TotalSpent,
                    AvgTicket = Math.Round(whale.AvgTicket, 2),
                    RetentionScore = retentionScore,
                    TrendData = trendData
                });
            }

            return result;
        }

        public async Task<WhaleHistoryDto?> GetWhaleHistoryAsync(int walletId)
        {
            // 1. Lấy toàn bộ lịch sử không lọc từ Repo
            var txs = await _transactionRepo.GetTransactionsByWalletIdAsync(walletId);
            if (txs == null || !txs.Any()) return null;

            var firstTx = txs.First();
            var successTxs = txs.Where(t => t.Status == "Success").ToList();

            decimal totalDebit = 0;
            decimal totalRefund = 0;

            // Định nghĩa danh sách các loại danh mục cho Pie Chart
            var distributionDict = new Dictionary<string, decimal>();

            // 2. Xử lý phân loại dòng tiền & Tính toán LTV (Total Spent) động
            foreach (var t in successTxs)
            {
                var descLower = (t.Description ?? "").ToLower();
                var type = t.Type ?? "";
                var refType = t.ReferenceType ?? "Khác";
                decimal absAmount = Math.Abs(t.Amount);

                // --- LUỒNG TRỪ TIỀN (USER CHI TIÊU) ---
                if (type == "Debit" ||
                    type == "System_Fee_Payment" ||
                    type == "Escrow_Hold" || // Tiền cọc giải thưởng Event cũng là tiền túi user bỏ ra
                    descLower.Contains("pay for") ||
                    descLower.Contains("paid entry fee") ||
                    descLower.Contains("deposit prize money"))
                {
                    totalDebit += absAmount;

                    // Phân loại danh mục hiển thị trên Pie Chart (Biểu đồ tròn)
                    string category = refType;
                    if (type == "System_Fee_Payment" || descLower.Contains("system fee"))
                    {
                        category = "Event Fee";
                    }
                    else if (type == "Escrow_Hold" || descLower.Contains("deposit prize money"))
                    {
                        category = "Event Prize Deposit";
                    }
                    else if (refType == "OrderPayment")
                    {
                        category = "Shopping";
                    }

                    if (!distributionDict.ContainsKey(category)) distributionDict[category] = 0;
                    distributionDict[category] += absAmount;
                }
                // --- LUỒNG HOÀN TIỀN (USER ĐƯỢC CỘNG LẠI TIỀN) ---
                else if (type == "Event_Refund" ||
                         refType == "Refund" ||
                         refType == "OrderRefund" ||
                         descLower.Contains("refund"))
                {
                    totalRefund += absAmount;

                    // Đưa tiền hoàn vào mục "Refund" trên Pie Chart để đối soát
                    string category = "Refund";
                    if (!distributionDict.ContainsKey(category)) distributionDict[category] = 0;
                    distributionDict[category] += absAmount;
                }
            }

            var netTotalSpent = totalDebit - totalRefund;

            // Chuyển đổi Dictionary thành List Dto cho Pie Chart
            var distributionData = distributionDict
                .Select(kv => new TransactionDistributionDto
                {
                    Name = kv.Key,
                    Value = kv.Value
                })
                .ToList();

            // 3. Map danh sách hiển thị bảng lịch sử chi tiết (Ép dấu âm/dương chuẩn trực quan)
            var transactionList = txs.Select(t =>
            {
                var displayAmount = t.Amount;
                var descLower = (t.Description ?? "").ToLower();
                var type = t.Type ?? "";

                // Logic hiển thị dấu âm (-) cho các khoản trừ tiền trên giao diện
                if (type == "Debit" ||
                    type == "System_Fee_Payment" ||
                    type == "Escrow_Hold" ||
                    descLower.Contains("pay for") ||
                    descLower.Contains("paid entry fee"))
                {
                    displayAmount = -Math.Abs(t.Amount);
                }
                // Logic hiển thị dấu dương (+) cho các khoản cộng/nạp tiền
                else if (type == "Credit" ||
                         type == "Event_Refund" ||
                         descLower.Contains("receive") ||
                         descLower.Contains("refund") ||
                         descLower.Contains("top up"))
                {
                    displayAmount = Math.Abs(t.Amount);
                }

                // Xác định phương thức thanh toán hiển thị trực quan
                string method = "Khác";
                if (type == "Debit" || type == "System_Fee_Payment" || type == "Escrow_Hold")
                {
                    method = "Ví cá nhân";
                }
                else if (descLower.Contains("vnpay"))
                {
                    method = "VNPAY";
                }
                else if (type == "Event_Refund" || t.ReferenceType == "Refund")
                {
                    method = "Hoàn vào ví";
                }

                return new WhaleTransactionDto
                {
                    Id = t.TransactionCode ?? $"TX-{t.TransactionId}",
                    Date = t.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    Feature = $"{t.ReferenceType} - {t.Description}",
                    Amount = displayAmount,
                    Status = t.Status,
                    Method = method
                };
            }).ToList();

            return new WhaleHistoryDto
            {
                Id = $"{walletId}",
                Name = firstTx.Wallet?.Account?.UserName ?? "Anonymous user",
                TotalSpent = netTotalSpent > 0 ? netTotalSpent : 0,
                DistributionData = distributionData,
                Transactions = transactionList
            };
        }

        #region Hàm tạo dải mốc thời gian liên tục
        private Dictionary<string, string> GenerateTimelineSlots(DateTime start, DateTime end, string type)
        {
            var slots = new Dictionary<string, string>();
            var current = start.Date;

            if (type == "monthly")
            {
                while (current <= end.Date)
                {
                    var key = current.ToString("yyyy-MM");
                    if (!slots.ContainsKey(key))
                        slots.Add(key, current.ToString("MMM yyyy", CultureInfo.InvariantCulture));
                    current = current.AddMonths(1);
                }
            }
            else
            {
                while (current <= end.Date)
                {
                    var key = current.ToString("yyyy-MM-dd");
                    if (!slots.ContainsKey(key))
                        slots.Add(key, current.ToString("dd MMM", CultureInfo.InvariantCulture));
                    current = current.AddDays(1);
                }
            }
            return slots;
        }

        private bool IsTransactionInSlot(DateTime txDate, string slotKey, string type)
        {
            return type == "monthly" ? txDate.ToString("yyyy-MM") == slotKey : txDate.ToString("yyyy-MM-dd") == slotKey;
        }
        #endregion
    }
}
