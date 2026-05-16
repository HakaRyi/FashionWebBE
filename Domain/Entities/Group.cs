namespace Domain.Entities;

public partial class Group
{
    public int GroupId { get; set; }

    public string? Name { get; set; }

    public bool? IsGroup { get; set; } = true; // true cho group, false cho 1-1 chat

    public int? CreateBy { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    public virtual ICollection<GroupUser> GroupUsers { get; set; } = new List<GroupUser>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<PinnedMessage> PinnedMessages { get; set; } = new List<PinnedMessage>();
    public virtual ICollection<Image> Images { get; set; } = new List<Image>();

}
