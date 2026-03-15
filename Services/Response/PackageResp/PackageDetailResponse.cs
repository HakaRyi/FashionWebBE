using Services.Response.FeatureResp;

namespace Services.Response.PackageResp
{
    public class PackageDetailResponse
    {
        public int PackageId { get; set; }
        public int AccountId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? CreatorName { get; set; }

        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }

        public List<FeatureResponse> Features { get; set; } = new();
    }
}
