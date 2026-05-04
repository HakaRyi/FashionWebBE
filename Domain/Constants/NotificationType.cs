namespace Domain.Constants
{
    public static class NotificationType
    {
        public const string OrderCreated = "OrderCreated";
        public const string OrderPaid = "OrderPaid";
        public const string OrderShipping = "OrderShipping";
        public const string OrderDelivered = "OrderDelivered";
        public const string OrderCompleted = "OrderCompleted";
        public const string OrderCancelled = "OrderCancelled";

        public const string RefundRequested = "RefundRequested";
        public const string RefundApproved = "RefundApproved";
        public const string RefundRejected = "RefundRejected";
    }
}