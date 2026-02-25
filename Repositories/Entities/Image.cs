using System;
using System.Collections.Generic;

namespace Repositories.Entities;

public partial class Image
{
    public int ImageId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public int? PostId { get; set; }
    public int? ItemId { get; set; }
    public int? AccountAvatarId { get; set; }
    public string? OwnerType { get; set; }
    public DateTime? CreatedAt { get; set; }

    public virtual Item? Item { get; set; } 

    public virtual Post? Post { get; set; } 
    public virtual Account? Account { get; set; }
}
