namespace Application.Response.FollowResp
{
    public class FollowResponse
    {
        public int FollowingId { get; set; }
        public int FollowerId { get; set; }
        public string FollowerAvatar { get; set; } = null!;
        public string FollowingAvatar { get; set; } = null!;
        public string FollowerName { get; set; } = null!;
        public string FollowingName { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
    }
}
