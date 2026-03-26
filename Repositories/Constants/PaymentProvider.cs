namespace Repositories.Constants
{
    public static class PaymentProvider
    {
        public const string VnPay = "VNPAY";
        public const string ZaloPay = "ZALOPAY";

        public static readonly List<string> All = new()
        {
            VnPay,
            ZaloPay
        };

        public static bool IsValid(string? provider)
        {
            return !string.IsNullOrWhiteSpace(provider) && All.Contains(provider);
        }
    }
}