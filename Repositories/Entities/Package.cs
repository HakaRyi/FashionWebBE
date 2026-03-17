namespace Repositories.Entities;

public partial class Package
{
    public int PackageId { get; set; }
    public int AccountId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }

    public decimal Price { get; set; }
    public int DurationDays { get; set; }

    public bool? IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<AccountSubscription> Subscriptions { get; set; } = new List<AccountSubscription>();

    public virtual ICollection<PackageFeature> PackageFeatures { get; set; } = new List<PackageFeature>();
}