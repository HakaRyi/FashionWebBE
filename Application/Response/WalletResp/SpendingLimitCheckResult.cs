namespace Application.Response.WalletResp
{
    public class SpendingLimitCheckResult
    {
        public bool IsAllowed { get; set; }
        public bool IsWarning { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal CurrentSpent { get; set; }
        public decimal ProjectedSpent { get; set; }
        public decimal LimitAmount { get; set; }
        public decimal WarningThresholdPercent { get; set; }
    }
}
