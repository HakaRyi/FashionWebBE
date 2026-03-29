namespace Repositories.Constants
{
    public static class TransactionType
    {
        public const string Credit = "Credit";
        public const string Debit = "Debit";

        public static readonly List<string> All = new()
        {
            Credit,
            Debit
        };

        public static bool IsValid(string? type)
        {
            return !string.IsNullOrWhiteSpace(type) && All.Contains(type);
        }
    }
}