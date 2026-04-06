namespace Domain.Constants
{
    public static class EscrowStatus
    {
        public const string Held = "Held";
        public const string Released = "Released";
        public const string Refunded = "Refunded";

        public static readonly List<string> All = new()
        {
            Held,
            Released,
            Refunded
        };

        public static bool IsValid(string? status)
        {
            return !string.IsNullOrWhiteSpace(status) && All.Contains(status);
        }
    }
}