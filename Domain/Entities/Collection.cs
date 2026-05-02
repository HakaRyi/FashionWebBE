using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities;

public partial class Collection
{
    public int CollectionId { get; set; }

    public int AccountId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Account Account { get; set; } = null!;
    public virtual ICollection<CollectionItem> CollectionItems { get; set; } = new List<CollectionItem>();
}
