namespace Domain.Entities
{
    public partial class ItemVariant
    {
        public int ItemVariantId { get; set; }

        public int ItemId { get; set; }

        /// <summary>
        /// SKU is used to identify one sellable variant.
        /// Example: TSHIRT-BLACK-M
        /// </summary>
        public string Sku { get; set; } = null!;

        /// <summary>
        /// Example: S, M, L, XL, 39, 40...
        /// </summary>
        public string? SizeCode { get; set; }

        /// <summary>
        /// Optional color value for commercial use.
        /// </summary>
        public string? Color { get; set; }

        /// <summary>
        /// Actual sale price of this variant.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Real stock quantity in system.
        /// </summary>
        public int StockQuantity { get; set; }

        /// <summary>
        /// Quantity temporarily held by pending orders.
        /// </summary>
        public int ReservedQuantity { get; set; }

        public ItemVariantStatus Status { get; set; } = ItemVariantStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public virtual Item Item { get; set; } = null!;

        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }

    public enum ItemVariantStatus
    {
        Inactive = 0,
        Active = 1,
        OutOfStock = 2,
        Archived = 3,
        Deleted = 4
    }
}