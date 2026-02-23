using System;
using System.Collections.Generic;

namespace Repositories.Entities;

public partial class Image
{
    public int ImageId { get; set; }

    public int OwnerId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public string? OwnerType { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Item Owner { get; set; } = null!;

    public virtual Post OwnerNavigation { get; set; } = null!;
}
