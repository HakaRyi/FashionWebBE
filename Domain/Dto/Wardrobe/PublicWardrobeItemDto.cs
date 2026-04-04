namespace Domain.Dto.Wardrobe
{
    public class PublicWardrobeItemDto
    {
        public int ItemId { get; set; }
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
        public string? Size { get; set; }
        public string? Brand { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? ThumbnailUrl { get; set; }
    }
}