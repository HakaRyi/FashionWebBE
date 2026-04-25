namespace Application.Request.OrderReq
{
    public class CreateRefundRequest
    {
        public string Reason { get; set; } = string.Empty;
        public string? Proof1 { get; set; }
        public string? Proof2 { get; set; }
    }
}