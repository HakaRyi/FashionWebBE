namespace Domain.Dto.Wallet
{
    public class ExpenseByReferenceTypeResponseDto
    {
        public string ReferenceType { get; set; } = null!;
        public decimal Amount { get; set; }
        public int TransactionCount { get; set; }
    }
}