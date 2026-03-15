using Services.Response.FeatureResp;

namespace Services.Response.PackageResp
{
    public class PackageResponse
    {
        public int PackageId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public DateTime? CreatedAt { get; set; }

        public List<FeatureResponse> Features { get; set; } = new();
    }
}
