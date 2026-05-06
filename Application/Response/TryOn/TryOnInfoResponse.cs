namespace Application.Response.TryOn
{
    public class TryOnInfoResponse
    {
        public decimal TryOnPrice { get; set; }
        public decimal Balance { get; set; }
        public decimal LockedBalance { get; set; }
        public decimal AvailableBalance { get; set; }
        public bool CanTryOn { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}