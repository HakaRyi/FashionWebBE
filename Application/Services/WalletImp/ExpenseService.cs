using Application.Interfaces;
using Application.Request.WalletReq;
using Application.Response.WalletResp;
using Domain.Constants;
using Domain.Dto.Common;
using Domain.Dto.Wallet;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.WalletImp
{
    public class ExpenseService : IExpenseService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IWalletRepository _walletRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ExpenseService(
            ITransactionRepository transactionRepository,
            IWalletRepository walletRepository,
            IUnitOfWork unitOfWork)
        {
            _transactionRepository = transactionRepository;
            _walletRepository = walletRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResultDto<TransactionResponseDto>> GetMyTransactionsAsync(
            int accountId,
            TransactionFilterRequestDto request)
        {
            request.Page = request.Page <= 0 ? 1 : request.Page;
            request.PageSize = request.PageSize <= 0 ? 20 : request.PageSize;

            ValidateTransactionFilter(request);

            var query = _transactionRepository.Query()
                .Where(x => x.Wallet.AccountId == accountId);

            if (!string.IsNullOrWhiteSpace(request.Type))
            {
                query = query.Where(x => x.Type == request.Type);
            }

            if (!string.IsNullOrWhiteSpace(request.ReferenceType))
            {
                query = query.Where(x => x.ReferenceType == request.ReferenceType);
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                query = query.Where(x => x.Status == request.Status);
            }

            if (request.FromDate.HasValue)
            {
                var from = request.FromDate.Value.Date;
                query = query.Where(x => x.CreatedAt >= from);
            }

            if (request.ToDate.HasValue)
            {
                var to = request.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(x => x.CreatedAt <= to);
            }

            if (request.MinAmount.HasValue)
            {
                query = query.Where(x => x.Amount >= request.MinAmount.Value);
            }

            if (request.MaxAmount.HasValue)
            {
                query = query.Where(x => x.Amount <= request.MaxAmount.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim();
                query = query.Where(x => x.Description != null && x.Description.Contains(keyword));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new TransactionResponseDto
                {
                    TransactionId = x.TransactionId,
                    WalletId = x.WalletId,
                    PaymentId = x.PaymentId,
                    TransactionCode = x.TransactionCode,
                    Amount = x.Amount,
                    BalanceBefore = x.BalanceBefore,
                    BalanceAfter = x.BalanceAfter,
                    Type = x.Type,
                    ReferenceType = x.ReferenceType,
                    ReferenceId = x.ReferenceId,
                    Description = x.Description,
                    CreatedAt = x.CreatedAt,
                    Status = x.Status
                })
                .ToListAsync();

            return new PagedResultDto<TransactionResponseDto>
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                HasMore = request.Page * request.PageSize < totalCount
            };
        }

        public async Task<TransactionDetailResponseDto> GetTransactionDetailAsync(
            int accountId,
            int transactionId)
        {
            var transaction = await _transactionRepository.GetByIdWithWalletAsync(transactionId);

            if (transaction == null || transaction.Wallet.AccountId != accountId)
            {
                throw new KeyNotFoundException("Không tìm thấy giao dịch.");
            }

            return new TransactionDetailResponseDto
            {
                TransactionId = transaction.TransactionId,
                WalletId = transaction.WalletId,
                PaymentId = transaction.PaymentId,
                TransactionCode = transaction.TransactionCode,
                Amount = transaction.Amount,
                BalanceBefore = transaction.BalanceBefore,
                BalanceAfter = transaction.BalanceAfter,
                Type = transaction.Type,
                ReferenceType = transaction.ReferenceType,
                ReferenceId = transaction.ReferenceId,
                Description = transaction.Description,
                CreatedAt = transaction.CreatedAt,
                Status = transaction.Status,
                SourceName = transaction.ReferenceType,
                SourceCode = transaction.ReferenceId?.ToString(),
                DisplayTitle = BuildDisplayTitle(transaction)
            };
        }

        public async Task<ExpenseSummaryResponseDto> GetMyExpenseSummaryAsync(
            int accountId,
            ExpenseSummaryRequestDto request)
        {
            ValidateMonthYear(request.Month, request.Year);

            var wallet = await _walletRepository.GetByAccountIdAsync(accountId);
            if (wallet == null)
            {
                throw new KeyNotFoundException("Không tìm thấy ví.");
            }

            var query = _transactionRepository.Query()
                .Where(x => x.Wallet.AccountId == accountId
                         && x.CreatedAt.Month == request.Month
                         && x.CreatedAt.Year == request.Year
                         && x.Status == TransactionStatus.Success);

            var totalIncome = await query
                .Where(x => x.Type == TransactionType.Credit)
                .SumAsync(x => (decimal?)x.Amount) ?? 0;

            var totalExpense = await query
                .Where(x => x.Type == TransactionType.Debit)
                .SumAsync(x => (decimal?)x.Amount) ?? 0;

            var totalTransactions = await query.CountAsync();

            return new ExpenseSummaryResponseDto
            {
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                NetAmount = totalIncome - totalExpense,
                TotalTransactions = totalTransactions,
                CurrentBalance = wallet.Balance,
                CurrentLockedBalance = wallet.LockedBalance,
                Currency = wallet.Currency ?? "VND"
            };
        }

        public async Task<List<ExpenseByReferenceTypeResponseDto>> GetExpenseByReferenceTypeAsync(
            int accountId,
            ExpenseByReferenceTypeRequestDto request)
        {
            ValidateMonthYear(request.Month, request.Year);

            var type = string.IsNullOrWhiteSpace(request.Type)
                ? TransactionType.Debit
                : request.Type;

            if (!TransactionType.IsValid(type))
            {
                throw new ArgumentException("Type không hợp lệ.");
            }

            return await _transactionRepository.Query()
                .Where(x => x.Wallet.AccountId == accountId
                         && x.CreatedAt.Month == request.Month
                         && x.CreatedAt.Year == request.Year
                         && x.Status == TransactionStatus.Success
                         && x.Type == type)
                .GroupBy(x => x.ReferenceType)
                .Select(g => new ExpenseByReferenceTypeResponseDto
                {
                    ReferenceType = g.Key,
                    Amount = g.Sum(x => x.Amount),
                    TransactionCount = g.Count()
                })
                .OrderByDescending(x => x.Amount)
                .ToListAsync();
        }

        public async Task<List<CashflowPointResponseDto>> GetCashflowAsync(
            int accountId,
            CashflowRequestDto request)
        {
            if (request.FromDate > request.ToDate)
            {
                throw new ArgumentException("FromDate không được lớn hơn ToDate.");
            }

            var groupBy = request.GroupBy?.Trim().ToLower() ?? "day";
            if (groupBy != "day" && groupBy != "month")
            {
                throw new ArgumentException("GroupBy chỉ chấp nhận 'day' hoặc 'month'.");
            }

            var from = request.FromDate.Date;
            var to = request.ToDate.Date.AddDays(1).AddTicks(-1);

            var transactions = await _transactionRepository.Query()
                .Where(x => x.Wallet.AccountId == accountId
                         && x.CreatedAt >= from
                         && x.CreatedAt <= to
                         && x.Status == TransactionStatus.Success)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();

            if (groupBy == "month")
            {
                return transactions
                    .GroupBy(x => new { x.CreatedAt.Year, x.CreatedAt.Month })
                    .Select(g => new CashflowPointResponseDto
                    {
                        Period = $"{g.Key.Year:D4}-{g.Key.Month:D2}",
                        Income = g.Where(x => x.Type == TransactionType.Credit).Sum(x => x.Amount),
                        Expense = g.Where(x => x.Type == TransactionType.Debit).Sum(x => x.Amount),
                        NetAmount = g.Where(x => x.Type == TransactionType.Credit).Sum(x => x.Amount)
                                  - g.Where(x => x.Type == TransactionType.Debit).Sum(x => x.Amount)
                    })
                    .OrderBy(x => x.Period)
                    .ToList();
            }

            return transactions
                .GroupBy(x => x.CreatedAt.Date)
                .Select(g => new CashflowPointResponseDto
                {
                    Period = g.Key.ToString("yyyy-MM-dd"),
                    Income = g.Where(x => x.Type == TransactionType.Credit).Sum(x => x.Amount),
                    Expense = g.Where(x => x.Type == TransactionType.Debit).Sum(x => x.Amount),
                    NetAmount = g.Where(x => x.Type == TransactionType.Credit).Sum(x => x.Amount)
                              - g.Where(x => x.Type == TransactionType.Debit).Sum(x => x.Amount)
                })
                .OrderBy(x => x.Period)
                .ToList();
        }

        private static void ValidateTransactionFilter(TransactionFilterRequestDto request)
        {
            if (!string.IsNullOrWhiteSpace(request.Type) && !TransactionType.IsValid(request.Type))
            {
                throw new ArgumentException("Type không hợp lệ.");
            }

            if (!string.IsNullOrWhiteSpace(request.ReferenceType) &&
                !TransactionReferenceType.IsValid(request.ReferenceType))
            {
                throw new ArgumentException("ReferenceType không hợp lệ.");
            }

            if (!string.IsNullOrWhiteSpace(request.Status) &&
                !TransactionStatus.IsValid(request.Status))
            {
                throw new ArgumentException("Status không hợp lệ.");
            }

            if (request.FromDate.HasValue && request.ToDate.HasValue &&
                request.FromDate.Value > request.ToDate.Value)
            {
                throw new ArgumentException("FromDate không được lớn hơn ToDate.");
            }

            if (request.MinAmount.HasValue && request.MaxAmount.HasValue &&
                request.MinAmount.Value > request.MaxAmount.Value)
            {
                throw new ArgumentException("MinAmount không được lớn hơn MaxAmount.");
            }
        }

        private static void ValidateMonthYear(int month, int year)
        {
            if (month < 1 || month > 12)
            {
                throw new ArgumentException("Month phải từ 1 đến 12.");
            }

            if (year < 2000 || year > 3000)
            {
                throw new ArgumentException("Year không hợp lệ.");
            }
        }

        public async Task<SpendingLimitResponseDto> GetMySpendingLimitAsync(
    int accountId,
    int month,
    int year)
        {
            ValidateMonthYear(month, year);

            var wallet = await _walletRepository.GetByAccountIdAsync(accountId);
            if (wallet == null)
            {
                throw new KeyNotFoundException("Không tìm thấy ví.");
            }

            var spentThisMonth = await _transactionRepository.Query()
                .Where(x => x.Wallet.AccountId == accountId
                         && x.CreatedAt.Month == month
                         && x.CreatedAt.Year == year
                         && x.Status == TransactionStatus.Success
                         && x.Type == TransactionType.Debit)
                .SumAsync(x => (decimal?)x.Amount) ?? 0;

            var limitAmount = wallet.MonthlySpendingLimit;
            var remainingAmount = 0m;
            var usedPercent = 0m;
            var isExceeded = false;
            var isWarning = false;

            if (limitAmount.HasValue && limitAmount.Value > 0)
            {
                remainingAmount = limitAmount.Value - spentThisMonth;
                if (remainingAmount < 0)
                {
                    remainingAmount = 0;
                }

                usedPercent = spentThisMonth * 100 / limitAmount.Value;
                isExceeded = spentThisMonth > limitAmount.Value;
                isWarning = usedPercent >= wallet.SpendingWarningThresholdPercent;
            }

            return new SpendingLimitResponseDto
            {
                MonthlySpendingLimit = wallet.MonthlySpendingLimit,
                IsHardSpendingLimit = wallet.IsHardSpendingLimit,
                SpendingWarningThresholdPercent = wallet.SpendingWarningThresholdPercent,
                SpentThisMonth = spentThisMonth,
                RemainingAmount = remainingAmount,
                UsedPercent = usedPercent,
                IsExceeded = isExceeded,
                IsWarning = isWarning,
                Currency = wallet.Currency ?? "VND"
            };
        }

        public async Task<SpendingLimitResponseDto> UpdateMySpendingLimitAsync(
    int accountId,
    UpdateSpendingLimitRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentException("Dữ liệu cập nhật không hợp lệ.");
            }

            if (request.MonthlySpendingLimit.HasValue && request.MonthlySpendingLimit.Value < 0)
            {
                throw new ArgumentException("MonthlySpendingLimit không được nhỏ hơn 0.");
            }

            if (request.SpendingWarningThresholdPercent <= 0 ||
                request.SpendingWarningThresholdPercent > 100)
            {
                throw new ArgumentException("SpendingWarningThresholdPercent phải từ 1 đến 100.");
            }

            var wallet = await _walletRepository.GetByAccountIdAsync(accountId);
            if (wallet == null)
            {
                throw new KeyNotFoundException("Không tìm thấy ví.");
            }

            wallet.MonthlySpendingLimit = request.MonthlySpendingLimit;
            wallet.IsHardSpendingLimit = request.IsHardSpendingLimit;
            wallet.SpendingWarningThresholdPercent = request.SpendingWarningThresholdPercent;
            wallet.UpdatedAt = DateTime.UtcNow;

            _walletRepository.Update(wallet);
            await _unitOfWork.SaveChangesAsync();

            var now = DateTime.UtcNow;
            return await GetMySpendingLimitAsync(accountId, now.Month, now.Year);
        }

        private static string BuildDisplayTitle(Transaction transaction)
        {
            return transaction.ReferenceType switch
            {
                TransactionReferenceType.TopUp => $"Nạp tiền #{transaction.ReferenceId}",
                TransactionReferenceType.OrderPayment => $"Thanh toán đơn hàng #{transaction.ReferenceId}",
                TransactionReferenceType.OrderRefund => $"Hoàn tiền đơn hàng #{transaction.ReferenceId}",
                TransactionReferenceType.TryOn => $"Thanh toán Try-On #{transaction.ReferenceId}",
                TransactionReferenceType.EventReward => $"Thưởng sự kiện #{transaction.ReferenceId}",
                TransactionReferenceType.Withdraw => $"Rút tiền #{transaction.ReferenceId}",
                TransactionReferenceType.Adjustment => $"Điều chỉnh số dư #{transaction.ReferenceId}",
                _ => $"Giao dịch #{transaction.TransactionId}"
            };
        }
    }
}