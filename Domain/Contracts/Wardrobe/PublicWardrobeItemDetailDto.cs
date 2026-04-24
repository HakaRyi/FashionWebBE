using Application.Response.ItemResp;

namespace Domain.Contracts.Wardrobe
{
    public class PublicWardrobeItemDetailDto
    {
        public int ItemId { get; set; }
        public int WardrobeId { get; set; }
        public int AccountId { get; set; }

        public string? ItemName { get; set; }
        public string? ItemType { get; set; }
        public string? Category { get; set; }
        public string? SubCategory { get; set; }
        public string? Style { get; set; }
        public string? Gender { get; set; }
        public string? MainColor { get; set; }
        public string? SubColor { get; set; }
        public string? Material { get; set; }
        public string? Pattern { get; set; }
        public string? Fit { get; set; }
        public string? Neckline { get; set; }
        public string? SleeveLength { get; set; }
        public string? Length { get; set; }
        public string? Size { get; set; }
        public string? Brand { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }

        public List<string> ImageUrls { get; set; } = new();

        public string? OwnerUserName { get; set; }
        public string? OwnerAvatarUrl { get; set; }

        public bool IsForSale { get; set; }
        public decimal? ListedPrice { get; set; }
        public string? Condition { get; set; }
        public DateTime? PublishedAt { get; set; }

        public List<ItemVariantResponseDto> Variants { get; set; } = new();
    }
}