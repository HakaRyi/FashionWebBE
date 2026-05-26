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

        public const string LikePost = "LIKE_POST";

        public const string CommentPost = "COMMENT_POST";

        public const string MentionPost = "MENTION_POST";

        public const string ReplyComment = "REPLY_COMMENT";

        public const string SharePost = "SHARE_POST";

        public const string FollowUser = "FOLLOW_USER";

        public const string SavePost = "SAVE_POST";
    }
}