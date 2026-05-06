using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities;

public partial class CollectionItem
{
    public int CollectionId { get; set; }
    public virtual Collection Collection { get; set; } = null!;

    public int ItemId { get; set; }
    public virtual Item Item { get; set; } = null!;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
