namespace Repositories.Entities;

public partial class Post
{
    public int PostId { get; set; }

    public int AccountId { get; set; }

    public int? EventId { get; set; }

    public string? Tittle { get; set; }

    public string? Content { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsExpertPost { get; set; }

    public string? Status { get; set; }

    public double? Score { get; set; }

    public int? LikeCount { get; set; }

    public int? ShareCount { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual Event? Event { get; set; }

    public virtual ICollection<Image> Images { get; set; } = new List<Image>();

    public virtual PostVector? PostVector { get; set; }

    public virtual ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();

    public virtual ICollection<UserReport> UserReports { get; set; } = new List<UserReport>();
}
