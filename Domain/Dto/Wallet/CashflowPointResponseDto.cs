namespace Domain.Dto.Wallet
{
    public class CashflowPointResponseDto
    {
        public string Period { get; set; } = null!;
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
        public decimal NetAmount { get; set; }
    }
}