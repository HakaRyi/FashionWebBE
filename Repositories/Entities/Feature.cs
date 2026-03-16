using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Entities
{
    public partial class Feature
    {
        public int FeatureId { get; set; }
        public string FeatureCode { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }

        public virtual ICollection<PackageFeature> PackageFeatures { get; set; } = new List<PackageFeature>();
    }
}
