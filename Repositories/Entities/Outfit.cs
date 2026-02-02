using System;
using System.Collections.Generic;

namespace Repositories.Entities;

public partial class Outfit
{
    public int OutfitId { get; set; }

    public int AccountId { get; set; }

    public string? OutfitName { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
}
