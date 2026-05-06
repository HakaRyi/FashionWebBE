namespace Domain.Constants
{
    public static class ReportStatus
    {
        public const string Pending = "Pending";
        public const string Reviewing = "Reviewing";
        public const string Resolved = "Resolved";  // report đúng, đã xử lý
        public const string Rejected = "Rejected";  // report sai / bị bác bỏ

        public static readonly List<string> AllStatuses = new()
        {
            Pending,
            Reviewing,
            Resolved,
            Rejected
        };

        public static bool IsValid(string status)
        {
            return AllStatuses.Contains(status);
        }

        public static bool CanTransition(string currentStatus, string newStatus)
        {
            return currentStatus switch
            {
                Pending => newStatus == Reviewing || newStatus == Resolved || newStatus == Rejected,
                Reviewing => newStatus == Resolved || newStatus == Rejected,
                Resolved => false,
                Rejected => false,
                _ => false
            };
        }
    }
}