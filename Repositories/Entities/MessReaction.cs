namespace Repositories.Entities;

public partial class MessReaction
{
    public int ReactId { get; set; }

    public string? Type { get; set; }

    public int? AccountReactId { get; set; }

    public int? MessageId { get; set; }

    public virtual Account? AccountReact { get; set; }

    public virtual Message? Message { get; set; }
}
