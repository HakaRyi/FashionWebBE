namespace Domain.Dto.Wallet
{
    public class ExpenseByReferenceTypeRequestDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string? Type { get; set; } = "Debit";
    }
}