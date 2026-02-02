using System;
using System.Collections.Generic;

namespace Repositories.Entities;

public partial class Package
{
    public int PackageId { get; set; }

    public int AccountId { get; set; }

    public string? Name { get; set; }

    public int CoinAmount { get; set; }

    public int PriceVnd { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
