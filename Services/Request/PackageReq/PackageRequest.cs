namespace Services.Request.PackageReq
{
    public class PackageRequest
    {
        public string? Name { get; set; }

        public int CoinAmount { get; set; }

        public int PriceVnd { get; set; }

        public bool? IsActive { get; set; }
    }
}
