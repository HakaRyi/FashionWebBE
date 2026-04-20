namespace Application.Response.WalletResp
{
    public class SpendingLimitResponseDto
    {
        public decimal? MonthlySpendingLimit { get; set; }
        public bool IsHardSpendingLimit { get; set; }
        public decimal SpendingWarningThresholdPercent { get; set; }
        public decimal SpentThisMonth { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal UsedPercent { get; set; }
        public bool IsExceeded { get; set; }
        public bool IsWarning { get; set; }
        public string Currency { get; set; } = "VND";
    }
}