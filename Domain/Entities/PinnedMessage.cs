namespace Domain.Entities;

public partial class PinnedMessage
{
    public int PinnedMsgId { get; set; }

    public int? GroupId { get; set; }

    public int? AccountPinnedId { get; set; }

    public int? MessageId { get; set; }

    public DateTime? PinnedAt { get; set; }

    public virtual Account? AccountPinned { get; set; }

    public virtual Group? Group { get; set; }

    public virtual Message? Message { get; set; }
}
