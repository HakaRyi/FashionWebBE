namespace Application.Response.ItemResp
{
    public class ItemCommerceResponseDto
    {
        public int ItemId { get; set; }
        public string? ItemName { get; set; }
        public bool IsForSale { get; set; }
        public decimal? ListedPrice { get; set; }
        public string? Condition { get; set; }
        public DateTime? PublishedAt { get; set; }
        public List<ItemVariantResponseDto> Variants { get; set; } = new();
    }
}