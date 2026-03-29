namespace Repositories.Entities;

public partial class RefreshToken
{
    public int RefreshTokenId { get; set; }

    public string Token { get; set; } = null!;

    public int AccountId { get; set; }

    public DateTime ExpiryDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string DeviceInfo { get; set; } = null!;

    public string IpAddress { get; set; } = null!;

    public bool? IsAvailable { get; set; }

    public virtual Account Account { get; set; } = null!;
}
