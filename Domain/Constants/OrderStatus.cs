namespace Domain.Constants
{
    public static class OrderStatus
    {
        public const string PendingPayment = "PENDING_PAYMENT";
        public const string Processing = "PROCESSING";
        public const string Shipping = "SHIPPING";
        public const string Delivered = "DELIVERED";
        public const string Completed = "COMPLETED";
        public const string Cancelled = "CANCELLED";
        public const string Refunding = "REFUNDING";
        public const string Refunded = "REFUNDED";

        public const string ReturnPickedUp = "RETURN_PICKED_UP";
        public const string ReturnShipping = "RETURN_SHIPPING";
        public const string ReturnDelivered = "RETURN_DELIVERED";
        public const string ReturnCompleted = "RETURN_COMPLETED";

        public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
        {
            PendingPayment,
            Processing,
            Shipping,
            Delivered,
            Completed,
            Cancelled,
            
            Refunding,
            Refunded,
            ReturnPickedUp,
            ReturnShipping,
            ReturnDelivered,
            ReturnCompleted
        };

        public static bool IsValid(string status)
        {
            return !string.IsNullOrWhiteSpace(status) && All.Contains(status);
        }
    }
}