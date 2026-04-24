using Pgvector;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public partial class Item
    {
        public int ItemId { get; set; }

        public int WardrobeId { get; set; }

        public string? ItemName { get; set; }

        public string? ItemType { get; set; }

        public string? Category { get; set; }

        public string? SubCategory { get; set; }

        public string? Style { get; set; }

        public string? Gender { get; set; }

        public string? MainColor { get; set; }

        public string? SubColor { get; set; }

        public string? Material { get; set; }

        public string? Pattern { get; set; }

        public string? Fit { get; set; }

        public string? Neckline { get; set; }

        public string? SleeveLength { get; set; }

        public string? Length { get; set; }

        /// <summary>
        /// Original size information for wardrobe purpose.
        /// This field can still be used for simple display.
        /// Sellable items should mainly use variants for actual ordering.
        /// </summary>
        public string? Size { get; set; }

        public string? Brand { get; set; }

        public string? Description { get; set; }

        [Column(TypeName = "vector(768)")]
        public Vector ItemEmbedding { get; set; } = null!;

        public bool? IsPublic { get; set; }

        public ItemStatus? Status { get; set; } = ItemStatus.Active;

        /// <summary>
        /// This flag shows whether the item is available for selling.
        /// False means wardrobe only.
        /// True means this item can be used in shopping flow.
        /// </summary>
        public bool IsForSale { get; set; } = false;

        /// <summary>
        /// A simple listed price for display.
        /// The real sale price should come from variant when variants exist.
        /// </summary>
        public decimal? ListedPrice { get; set; }

        /// <summary>
        /// Optional condition for second-hand or resale flow.
        /// Example: New, Like New, Used.
        /// </summary>
        public string? Condition { get; set; }

        public DateTime? PublishedAt { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdateAt { get; set; }

        public virtual Wardrobe Wardrobe { get; set; } = null!;

        public virtual ICollection<Image> Images { get; set; } = new List<Image>();

        public virtual ICollection<OutfitItem> OutfitItems { get; set; } = new List<OutfitItem>();

        public virtual ICollection<SavedItem> SavedByUsers { get; set; } = new List<SavedItem>();

        public virtual ICollection<ItemVariant> ItemVariants { get; set; } = new List<ItemVariant>();

        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }

    public enum ItemStatus
    {
        Draft = 0,
        Active = 1,
        Inactive = 2,
        Archived = 3,
        Deleted = 4
    }
}