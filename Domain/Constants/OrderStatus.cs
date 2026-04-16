namespace Domain.Constants
{
    public static class OrderStatus
    {
        public const string PendingPayment = "PENDING";
        public const string Confirm = "CONFIRMED";
        public const string Processing = "PROCESSING";
        public const string Shipping = "SHIPPING";
        public const string Completed = "COMPLETED";
        public const string Cancelled = "CANCELLED";
        public const string Refunding = "REFUNDING";
        public const string Refunded = "REFUNDED";
        public const string Done = "DONE";

        public static readonly List<string> All = new()
        {
            PendingPayment,
            Confirm,
            Processing,
            Shipping,
            Completed,
            Cancelled,
            Refunded,
            Refunding,
            Done,
        };

        public static bool IsValid(string? status)
        {
            return !string.IsNullOrWhiteSpace(status) && All.Contains(status);
        }
    }
}