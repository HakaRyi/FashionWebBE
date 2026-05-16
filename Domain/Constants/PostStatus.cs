namespace Domain.Constants
{
    public static class PostStatus
    {
        public const string Draft = "Draft";
        public const string Verifying = "Verifying";
        public const string PendingAdmin = "PendingAdmin";
        public const string Published = "Published";
        public const string Rejected = "Rejected";
        public const string Deleted = "Deleted";
        public const string Banned = "Banned";

        public static readonly List<string> AllStatuses = new()
        {
            Draft,
            Verifying,
            PendingAdmin,
            Published,
            Rejected,
            Deleted,
            Banned,
        };

        public static bool IsValid(string status)
        {
            return AllStatuses.Contains(status);
        }
    }
}