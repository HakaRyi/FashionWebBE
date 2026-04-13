namespace Application.Response.MessageResp
{
    public class MessageResponse
    {
        public int MessageId { get; set; }
        public string GroupName { get; set; }
        public int GroupId { get; set; }
        public string SenderName { get; set; }
        public string SenderAvatar { get; set; }    
        public int SenderId { get; set; }
        public string Content { get; set; } = string.Empty;
        public List<string>? Photos { get; set; } = new List<string>();
        public DateTime? SentAt { get; set; }
        public List<MessageReactionResponse>? Reactions { get; set; } = new List<MessageReactionResponse>();
        public int? ReplyToMessageId { get; set; }
    }
    public class MessageReactionResponse
    {
        public int ReactionId { get; set; }
        public int? AccountId { get; set; }
        public string AccountAvatar { get; set; }
        public string ReactionType { get; set; } = string.Empty;
    }
    public class PinMessageResponse
    {
        public int PinnedMsgId { get; set; }

        public int? GroupId { get; set; }

        public int? AccountPinnedId { get; set; }
        public string? AccountPinnedName { get; set; }
        public string? AccountPinnedAvatar { get; set; }
        public int? MessageId { get; set; }
        public string? MessageContent { get; set; }
        public List<string>? MessagePhotos { get; set; } = new List<string>();

        public DateTime? PinnedAt { get; set; }
    }
}
