using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Entities
{
    public partial class OutfitItem
    {
        public int OutfitId { get; set; }

        public virtual Outfit Outfit { get; set; } = null!;

        public int ItemId { get; set; }

        public virtual Item Item { get; set; } = null!;

        public string? Slot { get; set; }
    }
}
