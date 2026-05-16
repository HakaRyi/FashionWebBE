using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int SenderId { get; set; }

    [Column("target_user_id")]
    public int? TargetUserId { get; set; }

    public string? Title { get; set; }

    public string? Content { get; set; }

    public string? ImageUrl { get; set; }

    public string? Type { get; set; }

    public int? RelatedId { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Account Sender { get; set; } = null!;
}
