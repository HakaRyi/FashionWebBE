namespace Repositories.Entities;

public partial class Group
{
    public int GroupId { get; set; }

    public string? Name { get; set; }

    public bool? IsGroup { get; set; }

    public int? CreateBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<GroupUser> GroupUsers { get; set; } = new List<GroupUser>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<PinnedMessage> PinnedMessages { get; set; } = new List<PinnedMessage>();
}
