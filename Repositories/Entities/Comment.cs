namespace Repositories.Entities;

public partial class Comment
{
    public int CommentId { get; set; }

    public int PostId { get; set; }

    public int AccountId { get; set; }

    public int? ParentCommentId { get; set; }

    public string Content { get; set; } = null!;

    public int LikeCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Post Post { get; set; } = null!;

    public virtual Comment? ParentComment { get; set; }

    public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();

    public virtual ICollection<CommentReaction> Reactions { get; set; } = new List<CommentReaction>();
}

