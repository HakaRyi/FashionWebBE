namespace Domain.Entities;

public partial class Photo
{
    public int PhotoId { get; set; }

    public string? PhotoUrl { get; set; }

    public int? MessageId { get; set; }

    public virtual Message? Message { get; set; }
}
