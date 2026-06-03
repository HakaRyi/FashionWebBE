using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class OrderStatusHistory
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public string Status { get; set; } = null!;

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public string ActorType { get; set; } = null!;

        public int? ChangedById { get; set; }

        public string? Note { get; set; }

        public virtual Order Order { get; set; } = null!;
    }
}
