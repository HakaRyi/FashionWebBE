namespace Domain.Contracts
{
    public class UserWardrobeIntelDto
    {
        public int AccountId { get; set; }
        public int WardrobeId { get; set; }
        public int TotalItems { get; set; }
        public int ItemsForSale { get; set; }      
        public int ItemsForDisplayOnly { get; set; } 
        public Dictionary<string, int> StatusCounters { get; set; } = new();
        public int TotalVariants { get; set; }
        public int TotalStockQuantity { get; set; }
    }
}