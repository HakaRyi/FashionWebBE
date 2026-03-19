namespace Repositories.Constants
{
    public static class PostStatus
    {
        public const string Draft = "Draft";
        public const string Verifying = "Verifying";
        public const string PendingAdmin = "PendingAdmin";
        public const string Published = "Published";
        public const string Rejected = "Rejected";

        public static readonly List<string> AllStatuses = new()
        {
            Draft,
            Verifying,
            PendingAdmin,
            Published,
            Rejected
        };

        public static bool IsValid(string status)
        {
            return AllStatuses.Contains(status);
        }
    }
}