namespace Domain.Entities;

public partial class OrderDetail
{
    public int OrderDetailId { get; set; }
    public int OrderId { get; set; }

    public int? OutfitId { get; set; }
    public int? ItemId { get; set; }

    public string? ItemName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => UnitPrice * Quantity;
    public string? ImageUrl { get; set; }

    public virtual Order Order { get; set; } = null!;
    public virtual Outfit? Outfit { get; set; }
    public virtual Item? Item { get; set; }
}