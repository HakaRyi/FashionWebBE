using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Entities
{
    public class SystemSetting
    {
        public string SettingKey { get; set; } = null!;

        public string SettingValue { get; set; } = null!;

        public string? DataType { get; set; }

        public string? Description { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
