using System;
using System.Collections.Generic;

namespace Repositories.Entities;

public partial class Item
{
    public int ItemId { get; set; }

    public int WardrobeId { get; set; }

    public string? ItemName { get; set; }

    public string? Description { get; set; }

    public string? MainColor { get; set; }

    public string? Pattern { get; set; }

    public string? Style { get; set; }

    public string? Texture { get; set; }

    public string? Fabric { get; set; }

    public string? Brand { get; set; }

    public string? Placement { get; set; }

    public double? StyleScore { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Image> Images { get; set; } = new List<Image>();

    public virtual Wardrobe Wardrobe { get; set; } = null!;

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
}
