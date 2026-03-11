namespace Repositories.Entities;

public partial class Comment
{
    public int CommentId { get; set; }

    public int PostId { get; set; }

    public int AccountId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Post Post { get; set; } = null!;
}
