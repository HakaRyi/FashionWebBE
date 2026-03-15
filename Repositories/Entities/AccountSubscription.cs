using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Entities
{
    public partial class AccountSubscription
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int PackageId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }

        public virtual Account Account { get; set; } = null!;
        public virtual Package Package { get; set; } = null!;
    }
}
