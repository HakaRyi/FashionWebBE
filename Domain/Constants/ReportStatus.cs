namespace Domain.Constants
{
    public static class ReportStatus
    {
        public const string Pending = "Pending";
        public const string Reviewing = "Reviewing";
        public const string Resolved = "Resolved";
        public const string Rejected = "Rejected";

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
                Pending => newStatus == Reviewing || newStatus == Rejected,
                Reviewing => newStatus == Resolved || newStatus == Rejected,
                Resolved => false,
                Rejected => false,
                _ => false
            };
        }


        //Pending -> Reviewing
        //Reviewing -> Resolved
        //Reviewing -> Rejected

        //Pending: mới tạo
        //Reviewing: admin đang xem
        //Resolved: đã xử lý
        //Rejected: report không hợp lệ
    }
}