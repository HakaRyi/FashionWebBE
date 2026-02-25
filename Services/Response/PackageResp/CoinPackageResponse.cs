namespace Services.Response.PackageResp
{
    public class CoinPackageResponse
    {
        public int CoinPackageId { get; set; }
        public string PackageName { get; set; }
        public int CoinAmount { get; set; }
        public decimal Price { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
