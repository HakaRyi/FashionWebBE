namespace Application.Request.OufitReq
{
    public class OutfitRequest
    {
    }

    public class SaveOutfitRequestDto
    {
        public string? OutfitName { get; set; }
        public string? ImageUrl { get; set; }
        public List<OutfitItemRequestDto> Items { get; set; } = new();
    }

    public class OutfitItemRequestDto
    {
        public int ItemId { get; set; }

        public string? Slot { get; set; }
    }
}
