using Domain.Entities;

namespace Application.Response.ItemResp
{
    public class ItemVariantResponseDto
    {
        public int ItemVariantId { get; set; }
        public int ItemId { get; set; }
        public string Sku { get; set; } = null!;
        public string? SizeCode { get; set; }
        public string? Color { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int AvailableQuantity => StockQuantity - ReservedQuantity;
        public ItemVariantStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}