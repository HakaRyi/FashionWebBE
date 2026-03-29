namespace Services.Response.ItemResp
{
    public class ItemResponseDto
    {
        public int ItemId { get; set; }
        public string? ItemName { get; set; }
        public string? MainColor { get; set; }
        public string? Style { get; set; }
        public string? Brand { get; set; }
        public string? Fabric { get; set; }
        public string? Pattern { get; set; }
        public string? Texture { get; set; }
        public string? Placement { get; set; }
        public string? PrimaryImageUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ProductUploadDto
    {
        public string? ItemName { get; set; }
        //public int WardrobeId { get; set; }
        public string? Description { get; set; }
        public string? MainColor { get; set; }
        public string? Style { get; set; }
        public string? Fabric { get; set; }
        public string? Brand { get; set; }
        public string? Pattern { get; set; }
        public string? Texture { get; set; }
        public string? Placement { get; set; }
        public string? PrimaryImageUrl { get; set; }
        //public IFormFile? File { get; set; }
    }

    public class ItemDto
    {
        public int ItemId { get; set; }
        public string? ItemName { get; set; }
        public string? Description { get; set; }
        public string? MainColor { get; set; }
        public string? Brand { get; set; }
        public string? Status { get; set; }
        public string? ImageUrl { get; set; }
    }
}
