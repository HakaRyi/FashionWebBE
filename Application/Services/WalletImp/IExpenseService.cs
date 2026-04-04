using Domain.Dto.Common;
using Domain.Dto.Wallet;

namespace Application.Services.WalletImp
{
    public interface IExpenseService
    {
        Task<PagedResultDto<TransactionResponseDto>> GetMyTransactionsAsync(
            int accountId,
            TransactionFilterRequestDto request);

        Task<TransactionDetailResponseDto> GetTransactionDetailAsync(
            int accountId,
            int transactionId);

        Task<ExpenseSummaryResponseDto> GetMyExpenseSummaryAsync(
            int accountId,
            ExpenseSummaryRequestDto request);

        Task<List<ExpenseByReferenceTypeResponseDto>> GetExpenseByReferenceTypeAsync(
            int accountId,
            ExpenseByReferenceTypeRequestDto request);

        Task<List<CashflowPointResponseDto>> GetCashflowAsync(
            int accountId,
            CashflowRequestDto request);
    }
}