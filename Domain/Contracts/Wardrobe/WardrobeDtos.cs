namespace Domain.Dto.Wardrobe
{
    public class PublicProfileDto
    {
        public int AccountId { get; set; }
        public string? UserName { get; set; }
        public string? Description { get; set; }
        public int CountPost { get; set; }
        public int CountFollower { get; set; }
        public int CountFollowing { get; set; }
        public string? AvatarUrl { get; set; }
        public int TotalPublicItems { get; set; }
    }
}