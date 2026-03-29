namespace Repositories.Dto.Wallet
{
    public class CashflowRequestDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string GroupBy { get; set; } = "day";
    }
}