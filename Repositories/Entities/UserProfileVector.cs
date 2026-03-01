namespace Repositories.Entities;

public partial class UserProfileVector
{
    public int AccountId { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
}
