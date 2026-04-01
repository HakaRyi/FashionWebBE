namespace Services.Utils.Gateways
{
    public class VnPayOptions
    {
        public string TmnCode { get; set; } = string.Empty;
        public string HashSecret { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string Version { get; set; } = "2.1.0";
        public string Command { get; set; } = "pay";
        public string CurrCode { get; set; } = "VND";
        public string Locale { get; set; } = "vn";
        public string OrderType { get; set; } = "other";
        public string FallbackIpAddress { get; set; } = "127.0.0.1";
    }
}