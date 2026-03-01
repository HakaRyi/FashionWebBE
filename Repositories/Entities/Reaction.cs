namespace Repositories.Entities;

public partial class Reaction
{
    public int ReactionId { get; set; }

    public int PostId { get; set; }

    public int AccountId { get; set; }

    public string ReactionType { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Post Post { get; set; } = null!;
}
