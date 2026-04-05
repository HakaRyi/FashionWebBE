using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public partial class Model
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string? ImageUrl { get; set; }
    public string? Status { get; set; }
    [Column("create_at")]
    public DateTime? CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
}