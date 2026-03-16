namespace Repositories.Constants
{
    public static class PostStatus
    {
        public const string Draft = "Draft";
        public const string Verifying = "Verifying";
        public const string Published = "Published";
        public const string Rejected = "Rejected";      // AI reject hc admin khóa bài

        public static readonly List<string> AllStatuses = new()
        {
            Draft,
            Verifying,
            Published,
            Rejected
        };

        public static bool IsValid(string status)
        {
            return AllStatuses.Contains(status);
        }

        // 1 bài chỉ public khi post.Status == PostStatus.Published && post.Visibility == PostVisibility.Visible
    }
}