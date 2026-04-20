namespace Application.Request.WalletReq
{
    public class UpdateSpendingLimitRequestDto
    {
        public decimal? MonthlySpendingLimit { get; set; }
        public bool IsHardSpendingLimit { get; set; }
        public decimal SpendingWarningThresholdPercent { get; set; } = 80;
    }
}