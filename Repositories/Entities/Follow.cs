namespace Repositories.Entities
{
    public partial class Follow
    {
        public int UserId { get; set; }

        public int FollowerId { get; set; }

        public DateTime? CreatedAt { get; set; }

        public virtual Account User { get; set; } = null!;
        public virtual Account Follower { get; set; } = null!;
    }
}
