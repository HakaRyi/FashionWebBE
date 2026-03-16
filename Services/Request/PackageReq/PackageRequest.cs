namespace Services.Request.PackageReq
{
    public class PackageRequest
    {
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int DurationDays { get; set; }

        public int? CoinAmount { get; set; }

        public bool IsActive { get; set; }

        public List<FeatureAssignmentRequest>? Features { get; set; }
    }

    public class FeatureAssignmentRequest
    {
        public string FeatureCode { get; set; } = null!;
        public string Value { get; set; } = null!;
    }
}
