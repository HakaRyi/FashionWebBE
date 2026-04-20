namespace Application.Response.FollowResp
{
    public class ShareableUserResponse
    {
        public int AccountId { get; set; }

        public string? UserName { get; set; }

        public string? AvatarUrl { get; set; }

        public bool IsFollower { get; set; }

        public bool IsFollowing { get; set; }
    }
}