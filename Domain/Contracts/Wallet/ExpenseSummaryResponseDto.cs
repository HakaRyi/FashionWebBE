namespace Domain.Dto.Wallet
{
    public class ExpenseSummaryResponseDto
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetAmount { get; set; }

        public int TotalTransactions { get; set; }

        public decimal CurrentBalance { get; set; }
        public decimal CurrentLockedBalance { get; set; }
        public string Currency { get; set; } = "VND";
    }
}