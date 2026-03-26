namespace Services.Request.PaymentReq
{
    public class ZaloCallbackRequest
    {
        public string data { get; set; } = null!;
        public string? mac { get; set; }
        public int type { get; set; }
    }
}