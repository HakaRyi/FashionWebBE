namespace Domain.Constants
{
    public static class OrderStatus
    {
        public const string PendingPayment = "PENDING_PAYMENT";
        public const string Confirmed = "CONFIRMED";
        public const string Processing = "PROCESSING";
        public const string Shipping = "SHIPPING";
        public const string Delivered = "DELIVERED";
        public const string Completed = "COMPLETED";
        public const string Cancelled = "CANCELLED";
        public const string Refunding = "REFUNDING";
        public const string Refunded = "REFUNDED";

        public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
        {
            PendingPayment,
            Confirmed,
            Processing,
            Shipping,
            Delivered,
            Completed,
            Cancelled,
            Refunding,
            Refunded
        };

        public static bool IsValid(string status)
        {
            return !string.IsNullOrWhiteSpace(status) && All.Contains(status);
        }
    }
}