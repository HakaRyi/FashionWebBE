namespace Domain.Constants
{
    public static class TransactionReferenceType
    {
        public const string TopUp = "TopUp";
        public const string OrderPayment = "OrderPayment";
        public const string OrderRefund = "OrderRefund";
        public const string TryOn = "TryOn";
        public const string EventReward = "EventReward";
        public const string Withdraw = "Withdraw";
        public const string Adjustment = "Adjustment";

        public static readonly List<string> All = new()
        {
            TopUp,
            OrderPayment,
            OrderRefund,
            TryOn,
            EventReward,
            Withdraw,
            Adjustment
        };

        public static bool IsValid(string? referenceType)
        {
            return !string.IsNullOrWhiteSpace(referenceType) && All.Contains(referenceType);
        }
    }
}