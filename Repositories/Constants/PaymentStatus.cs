namespace Repositories.Constants
{
    public static class PaymentStatus
    {
        public const string Pending = "Pending";
        public const string Success = "Success";
        public const string Failed = "Failed";
        public const string Cancelled = "Cancelled";

        public static readonly List<string> All = new()
        {
            Pending,
            Success,
            Failed,
            Cancelled
        };

        public static bool IsValid(string? status)
        {
            return !string.IsNullOrWhiteSpace(status) && All.Contains(status);
        }
    }
}