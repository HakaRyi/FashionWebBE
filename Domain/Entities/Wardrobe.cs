namespace Domain.Entities;

public partial class Wardrobe
{
    public int WardrobeId { get; set; }

    public int AccountId { get; set; }

    public string? Name { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}
