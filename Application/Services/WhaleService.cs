using Application.Interfaces;
using Application.Response.TransactionResp;
using Domain.Interfaces;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class WhaleService : IWhaleService
    {
        private readonly ITransactionRepository _transactionRepo;

        private const string StatusSuccess = "Success";
        private const string RoleAdmin = "admin";
        private const string TypeDebit = "Debit";
        private const string TypeCredit = "Credit";
        private const string RefTypeTopUp = "TopUp";
        private const string RefTypeOrderRefund = "OrderRefund";
        private const string RefTypeEvent = "Event";

        public WhaleService(ITransactionRepository transactionRepo)
        {
            _transactionRepo = transactionRepo;
        }

        #region Core Financial Parser Engine
        private class FinancialFlow
        {
            public decimal RealDebit { get; set; }     // Dòng tiền thực tế chi ra khỏi ví
            public decimal RealRefund { get; set; }    // Dòng tiền thực tế được hoàn lại ví
            public bool IsValidOrder { get; set; }     // Đánh dấu giao dịch kinh doanh hợp lệ
            public string Category { get; set; } = "Khác";
            public string UIFormattedMethod { get; set; } = "Ví cá nhân";
        }

        private FinancialFlow ParseTransaction(Transaction t)
        {
            var flow = new FinancialFlow();
            if (t == null) return flow;

            var descLower = (t.Description ?? string.Empty).ToLowerInvariant();
            var refType = t.ReferenceType ?? string.Empty;
            decimal absAmount = Math.Abs(t.Amount);

            // 1. Loại bỏ nạp tiền VnPay
            if (string.Equals(refType, "TopUp", StringComparison.OrdinalIgnoreCase) || descLower.Contains("vnpay"))
            {
                flow.UIFormattedMethod = "VNPAY";
                return flow;
            }

            // 2. Xử lý các lệnh chi tiền (Debit hoặc Amount < 0)
            if (string.Equals(t.Type, "Debit", StringComparison.OrdinalIgnoreCase) || t.Amount < 0)
            {
                // QUAN TRỌNG: Loại bỏ lệnh Freeze ảo để tránh tính trùng 2 lần với lệnh Escrow/Fee kế tiếp
                if (descLower.Contains("freeze money") || descLower.Contains("locked balance ban đầu"))
                {
                    flow.IsValidOrder = false; // Không tính vào chi tiêu, không tính vào TxCount
                    return flow;
                }

                // Các lệnh chi thực tế (gồm phí tạo event và lệnh cắt tiền vào Escrow)
                flow.RealDebit = absAmount;
                flow.IsValidOrder = true;

                if (descLower.Contains("system creation fee"))
                {
                    flow.Category = "System Fee";
                    flow.UIFormattedMethod = "Phí nền tảng";
                }
                else if (descLower.Contains("transferred locked balance") || descLower.Contains("escrow"))
                {
                    flow.Category = "Event Operations";
                    flow.UIFormattedMethod = "Quỹ Sự Kiện";
                }
                else
                {
                    flow.Category = "Shopping";
                    flow.UIFormattedMethod = "Số dư ví";
                }
            }
            // 3. Xử lý các lệnh hoàn tiền (Credit hoặc Refund)
            else if (string.Equals(t.Type, "Credit", StringComparison.OrdinalIgnoreCase))
            {
                // Kiểm tra nếu là tiền hoàn từ Event
                if (descLower.Contains("refund of surplus") || descLower.Contains("refund"))
                {
                    flow.RealRefund = absAmount;
                    flow.Category = "Refund & Rewards";
                    flow.UIFormattedMethod = "Hoàn vào ví";
                }
                // Lưu ý: Người nhận giải thưởng (như 'minhquan' nhận 30,000) 
                // Đối với sàn thì đây là chi phí, nhưng đối với User thì đây là Thu nhập (Income), không phải Refund chi tiêu.
            }

            return flow;
        }

        private string GetUserNameFromTransaction(Transaction tx)
        {
            if (tx.Wallet?.Account != null && !string.IsNullOrEmpty(tx.Wallet.Account.UserName))
            {
                return tx.Wallet.Account.UserName;
            }
            return $"User_Wallet_{tx.WalletId}";
        }

        private string GetSlotKey(DateTime date, string intervalType)
        {
            return intervalType == "monthly" ? date.ToString("yyyy-MM") : date.ToString("yyyy-MM-dd");
        }
        #endregion

        #region Public API Services
        public async Task<List<WhaleDashboardDto>> GetTopWhalesAsync(DateTime fromDate, DateTime toDate, string? viewMode, string? searchQuery)
        {
            var endOfToDate = toDate.Date.AddDays(1).AddTicks(-1);
            var allTransactions = await _transactionRepo.GetAllTransactionsAsync(fromDate, endOfToDate);

            if (allTransactions == null || !allTransactions.Any()) return new List<WhaleDashboardDto>();

            // Lọc dữ liệu thô một lần duy nhất
            var validPaymentTxs = allTransactions
                .Where(t => string.Equals(t.Status, StatusSuccess, StringComparison.OrdinalIgnoreCase))
                .Where(t => !string.Equals(GetUserNameFromTransaction(t), RoleAdmin, StringComparison.OrdinalIgnoreCase))
                .ToList();

            string intervalType = !string.IsNullOrEmpty(viewMode)
                ? viewMode.ToLowerInvariant()
                : ((toDate - fromDate).TotalDays > 60 ? "monthly" : "daily");

            var timelineSlots = GenerateTimelineSlots(fromDate, toDate, intervalType);

            // --- TỐI ƯU HÓA O(N): PARSE 1 LẦN DUY NHẤT VÀ LƯU VÀO BỘ NHỚ ĐỆM ---
            var parsedTxs = validPaymentTxs.Select(t => new
            {
                Tx = t,
                UserName = GetUserNameFromTransaction(t),
                SlotKey = GetSlotKey(t.CreatedAt, intervalType),
                Flow = ParseTransaction(t)
            }).ToList();

            // 1. TÍNH TOÁN ĐƯỜNG TRUNG BÌNH TOÀN SÀN CHI TIÊU THỰC TẾ TRONG 1 PHÉP GROUP
            var platformAvgPerSlotDict = parsedTxs
                .GroupBy(p => p.SlotKey)
                .ToDictionary(
                    slotGroup => slotGroup.Key,
                    slotGroup =>
                    {
                        var walletNetSpent = slotGroup
                            .GroupBy(p => p.Tx.WalletId)
                            .Select(wGroup => wGroup.Sum(p => p.Flow.RealDebit - p.Flow.RealRefund))
                            .Where(net => net > 0)
                            .ToList();

                        return walletNetSpent.Any() ? Math.Round(walletNetSpent.Average(), 2) : 0;
                    }
                );

            // 2. LỌC THEO SEARCH QUERY
            var filteredParsedTxs = parsedTxs;
            if (!string.IsNullOrEmpty(searchQuery))
            {
                filteredParsedTxs = parsedTxs
                    .Where(p => p.UserName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // 3. GOM NHÓM TÍNH TOÁN CHỈ SỐ KHÁCH HÀNG VIP (WHALE)
            var whaleGroups = filteredParsedTxs
                .GroupBy(p => new { p.Tx.WalletId, Name = p.UserName })
                .Select(g =>
                {
                    decimal totalDebit = g.Sum(p => p.Flow.RealDebit);
                    decimal totalRefund = g.Sum(p => p.Flow.RealRefund);
                    int realPaymentCount = g.Count(p => p.Flow.IsValidOrder && p.Flow.RealDebit > 0);

                    decimal finalSpent = totalDebit - totalRefund;

                    return new
                    {
                        g.Key.WalletId,
                        g.Key.Name,
                        TotalSpent = finalSpent > 0 ? finalSpent : 0,
                        AvgTicket = realPaymentCount > 0 ? ((finalSpent > 0 ? finalSpent : totalDebit) / realPaymentCount) : 0,
                        TxCount = realPaymentCount,
                        TxList = g.ToList() // Giữ lại danh sách con để map xu hướng thần tốc
                    };
                })
                .Where(w => w.TotalSpent > 0 || w.TxCount > 0)
                .OrderByDescending(g => g.TotalSpent)
                .ToList();

            var result = new List<WhaleDashboardDto>();

            // 4. MAPPING DỮ LIỆU ĐƯỜNG XU HƯỚNG BẰNG DICTIONARY (Bỏ tư duy lặp xuyên dữ liệu)
            foreach (var whale in whaleGroups)
            {
                int retentionScore = Math.Min(100, whale.TxCount * 5 + 45);

                // Group các giao dịch cá nhân theo SlotKey trước
                var personalSlotDict = whale.TxList
                    .GroupBy(p => p.SlotKey)
                    .ToDictionary(
                        sg => sg.Key,
                        sg => sg.Sum(p => p.Flow.RealDebit - p.Flow.RealRefund)
                    );

                var trendData = new List<WhaleTrendDto>();

                foreach (var slot in timelineSlots)
                {
                    personalSlotDict.TryGetValue(slot.Key, out decimal netSpentInSlot);

                    trendData.Add(new WhaleTrendDto
                    {
                        Name = slot.Value,
                        Personal = netSpentInSlot > 0 ? netSpentInSlot : 0, // Tiền sạch, nếu âm/hoàn nhiều hơn chi thì hiển thị 0
                        TotalAvg = platformAvgPerSlotDict.TryGetValue(slot.Key, out var avg) ? avg : 0
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
            var txs = await _transactionRepo.GetTransactionsByWalletIdAsync(walletId);
            if (txs == null || !txs.Any()) return null;

            var firstTx = txs.First();
            var successTxs = txs.Where(t => string.Equals(t.Status, StatusSuccess, StringComparison.OrdinalIgnoreCase)).ToList();

            decimal totalDebit = 0;
            decimal totalRefund = 0;
            var distributionDict = new Dictionary<string, decimal>();

            foreach (var t in successTxs)
            {
                var flow = ParseTransaction(t);

                if (flow.RealDebit > 0)
                {
                    totalDebit += flow.RealDebit;
                    distributionDict[flow.Category] = distributionDict.GetValueOrDefault(flow.Category) + flow.RealDebit;
                }
                else if (flow.RealRefund > 0)
                {
                    totalRefund += flow.RealRefund;
                    distributionDict[flow.Category] = distributionDict.GetValueOrDefault(flow.Category) + flow.RealRefund;
                }
            }

            var netTotalSpent = totalDebit - totalRefund;
            var finalTotalSpent = netTotalSpent > 0 ? netTotalSpent : 0;

            var distributionData = distributionDict
                .Select(kv => new TransactionDistributionDto
                {
                    Name = kv.Key,
                    Value = kv.Value
                })
                .ToList();

            var transactionList = txs.Select(t =>
            {
                var flow = ParseTransaction(t);
                decimal displayAmount = t.Amount;

                if (flow.RealDebit > 0)
                {
                    displayAmount = -flow.RealDebit;
                }
                else if (string.Equals(t.ReferenceType, RefTypeTopUp, StringComparison.OrdinalIgnoreCase) || flow.RealRefund > 0)
                {
                    displayAmount = Math.Abs(t.Amount);
                }

                return new WhaleTransactionDto
                {
                    Id = !string.IsNullOrEmpty(t.TransactionCode) ? t.TransactionCode : $"TX-{t.TransactionId}",
                    Date = t.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    Feature = $"[{t.ReferenceType ?? "Khác"}] - {t.Description}",
                    Amount = displayAmount,
                    Status = t.Status,
                    Method = flow.UIFormattedMethod
                };
            }).ToList();

            return new WhaleHistoryDto
            {
                Id = $"{walletId}",
                Name = GetUserNameFromTransaction(firstTx),
                TotalSpent = finalTotalSpent,
                DistributionData = distributionData,
                Transactions = transactionList
            };
        }
        #endregion

        #region Timeline Slot Generator Helpers
        private Dictionary<string, string> GenerateTimelineSlots(DateTime start, DateTime end, string type)
        {
            var slots = new Dictionary<string, string>();
            var current = start.Date;

            if (type == "monthly")
            {
                while (current <= end.Date)
                {
                    var key = current.ToString("yyyy-MM");
                    slots.TryAdd(key, current.ToString("MMM yyyy", CultureInfo.InvariantCulture));
                    current = current.AddMonths(1);
                }
            }
            else
            {
                while (current <= end.Date)
                {
                    var key = current.ToString("yyyy-MM-dd");
                    slots.TryAdd(key, current.ToString("dd MMM", CultureInfo.InvariantCulture));
                    current = current.AddDays(1);
                }
            }
            return slots;
        }
        #endregion
    }
}