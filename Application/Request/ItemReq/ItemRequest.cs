using Domain.Entities;

namespace Application.Request.ItemReq
{
    public class ItemRequest
    {
    }

    public class ProductUploadDto
    {
        public string? ItemName { get; set; }

        //public int WardrobeId { get; set; }

        public string? ItemType { get; set; }

        public string? Category { get; set; }

        public string? SubCategory { get; set; }

        public string? Style { get; set; }

        public string? Gender { get; set; }

        public string? MainColor { get; set; }
        public string? Size { get; set; }

        public string? SubColor { get; set; }

        public string? Material { get; set; }

        public string? Pattern { get; set; }

        public string? Fit { get; set; }

        public string? Neckline { get; set; }

        public string? SleeveLength { get; set; }

        public string? Length { get; set; }

        public string? Brand { get; set; }

        public string? Description { get; set; }

        public bool? IsPublic { get; set; }

        public ItemStatus? Status { get; set; } = ItemStatus.Active;

        public string PrimaryImageUrl { get; set; }
    }

    public class SmartRecommendationRequestDto
    {
        public string Prompt { get; set; } = string.Empty;

        public int? ReferenceItemId { get; set; }

        //(SCOPE)
        public bool UseMyStylePreferences { get; set; }

        public bool UseMyPhysicalProfile { get; set; }

        public List<int> TargetWardrobeIds { get; set; } = new();

        //public List<int> TargetSavedItemIds { get; set; } = new();

        public bool IncludeMyWardrobe { get; set; } 
        public bool IncludeSavedItems { get; set; }

        public int Limit { get; set; } = 10;
    }
}
