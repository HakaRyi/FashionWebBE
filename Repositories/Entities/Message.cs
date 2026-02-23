using System;
using System.Collections.Generic;

namespace Repositories.Entities;

public partial class Message
{
    public int MessageId { get; set; }

    public int? AccountId { get; set; }

    public int? GroupId { get; set; }

    public int? ReplyToMessageId { get; set; }

    public bool? IsRecalled { get; set; }

    public DateTime? SentAt { get; set; }

    public string? Content { get; set; }

    public virtual Account? Account { get; set; }

    public virtual Group? Group { get; set; }

    public virtual ICollection<Message> InverseReplyToMessage { get; set; } = new List<Message>();

    public virtual ICollection<MessReaction> MessReactions { get; set; } = new List<MessReaction>();

    public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();

    public virtual ICollection<PinnedMessage> PinnedMessages { get; set; } = new List<PinnedMessage>();

    public virtual Message? ReplyToMessage { get; set; }
}
