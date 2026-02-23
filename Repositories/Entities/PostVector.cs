using System;
using System.Collections.Generic;

namespace Repositories.Entities;

public partial class PostVector
{
    public int PostId { get; set; }

    public string VectorData { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual Post Post { get; set; } = null!;
}
