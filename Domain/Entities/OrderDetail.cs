namespace Domain.Entities
{
    public partial class OrderDetail
    {
        public int OrderDetailId { get; set; }

        public int OrderId { get; set; }

        public int ItemId { get; set; }

        /// <summary>
        /// Nullable only for compatibility.
        /// In practice, sellable items should always order by variant.
        /// </summary>
        public int? ItemVariantId { get; set; }

        /// <summary>
        /// Snapshot fields are used so old orders still show correct data
        /// even if item information changes later.
        /// </summary>
        public string ItemNameSnapshot { get; set; } = null!;

        public string? VariantSnapshot { get; set; }

        public string? SkuSnapshot { get; set; }

        public string? ImageUrlSnapshot { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Stored total of this order line.
        /// It is better to save this value for reporting consistency.
        /// </summary>
        public decimal LineTotal { get; set; }

        public virtual Order Order { get; set; } = null!;

        public virtual Item Item { get; set; } = null!;

        public virtual ItemVariant? ItemVariant { get; set; }
    }
}