namespace Application.Request.OrderReq
{
    public class OrderFilterRequest
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public string? Status { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public string? SellerName { get; set; }

        public string? BuyerName { get; set; }

        public string? OrderCode { get; set; }
    }
}