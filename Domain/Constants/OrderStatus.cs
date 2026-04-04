namespace Domain.Constants
{
    public static class OrderStatus
    {
        public const string PendingPayment = "PendingPayment";
        public const string Processing = "Processing";
        public const string Shipping = "Shipping";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
        public const string Refunded = "Refunded";

        public static readonly List<string> All = new()
        {
            PendingPayment,
            Processing,
            Shipping,
            Completed,
            Cancelled,
            Refunded
        };

        public static bool IsValid(string? status)
        {
            return !string.IsNullOrWhiteSpace(status) && All.Contains(status);
        }
    }
}