namespace Services.Response.PackageResp
{
    public class CoinPackageDetail
    {
        public int PackageId { get; set; }

        public int AccountId { get; set; }

        public string? Name { get; set; }
        public string? CreateBy { get; set; }

        public int CoinAmount { get; set; }

        public int PriceVnd { get; set; }

        public bool? IsActive { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
