using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Entities
{
    public partial class TryOnHistory
    {
        public int TryOnId { get; set; }
        public int AccountId { get; set; }
        public string ImageUrl { get; set; }
        public string Status { get; set; }
        [Column("create_at")]
        public DateTime CreatedAt { get; set; }
        public virtual Account Account { get; set; }
    }
}
