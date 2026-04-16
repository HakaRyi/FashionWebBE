using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;

public partial class Account : IdentityUser<int>
{
    public DateTime? CreatedAt { get; set; }

    public string? Status { get; set; }

    public string? VerificationCode { get; set; }

    public DateTime? CodeExpiredAt { get; set; }

    public int FreeTryOn { get; set; }

    public string? Description { get; set; }

    public int CountPost { get; set; }

    public int CountFollower { get; set; }

    public int CountFollowing { get; set; }

    public string? IsOnline { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    public virtual ExpertProfile? ExpertProfile { get; set; }

    public virtual ICollection<GroupUser> GroupUsers { get; set; } = new List<GroupUser>();

    public virtual ICollection<MessReaction> MessReactions { get; set; } = new List<MessReaction>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Outfit> Outfits { get; set; } = new List<Outfit>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<PinnedMessage> PinnedMessages { get; set; } = new List<PinnedMessage>();

    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

    public ICollection<PostSave> SavedPosts { get; set; } = new List<PostSave>();

    public virtual ICollection<SavedItem> SavedItems { get; set; } = new List<SavedItem>();

    public virtual ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public virtual UserProfileVector? UserProfileVector { get; set; }

    public virtual ICollection<UserReport> UserReports { get; set; } = new List<UserReport>();

    public virtual Wardrobe? Wardrobe { get; set; }

    public virtual ICollection<EventWinner> EventWinners { get; set; } = new List<EventWinner>();

    public virtual ICollection<Follow> FollowUserNavigations { get; set; } = new List<Follow>();

    public virtual ICollection<Follow> FollowFollowerNavigations { get; set; } = new List<Follow>();

    public virtual ICollection<Image> Avatars { get; set; } = new List<Image>();

    public virtual ICollection<TryOnHistory> TryOnHistories { get; set; } = new List<TryOnHistory>();

    public virtual ICollection<Model> AccountModels { get; set; } = new List<Model>();

    public virtual Wallet Wallet { get; set; } = null!;

    public virtual ICollection<EscrowSession> SentEscrows { get; set; } = new List<EscrowSession>();

    public virtual ICollection<EscrowSession> ReceivedEscrows { get; set; } = new List<EscrowSession>();
}
