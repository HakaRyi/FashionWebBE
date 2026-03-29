namespace Services.Response.OrderResp
{
    public class OrderDetailResponse
    {
        public int OrderDetailId { get; set; }
        public int OrderId { get; set; }
        public int? OutfitId { get; set; }
        public int? ItemId { get; set; }
        public string? ItemName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string? ImageUrl { get; set; }
    }
}