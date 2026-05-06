namespace Application.Response.OrderResp
{
    public class OrderDetailResponse
    {
        public int OrderDetailId { get; set; }
        public int OrderId { get; set; }

        public int ItemId { get; set; }
        public int? ItemVariantId { get; set; }

        public string ItemName { get; set; } = null!;
        public string? VariantSnapshot { get; set; }
        public string? SkuSnapshot { get; set; }
        public string? ImageUrl { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}