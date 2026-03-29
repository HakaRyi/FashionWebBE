using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Entities
{
    public partial class SavedItem
    {
        public int AccountId { get; set; }

        public int ItemId { get; set; }

        public DateTime? SavedAt { get; set; } = DateTime.UtcNow;

        public virtual Account Account { get; set; } = null!;

        public virtual Item Item { get; set; } = null!;
    }
}
