namespace Application.Request.WalletReq
{
    public class WalletRequest
    {
    }

    public class TopUpRequest
    {
        public decimal Amount { get; set; }
        public string OrderCode { get; set; } = null!;
        public string Provider { get; set; } = null!;
    }
}
