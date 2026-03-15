using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Entities
{
    public partial class PackageFeature
    {
        public int PackageId { get; set; }
        public int FeatureId { get; set; }

        public string Value { get; set; }

        public virtual Package Package { get; set; } = null!;
        public virtual Feature Feature { get; set; } = null!;
    }
}
