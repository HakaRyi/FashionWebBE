using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Response.WardrobeResp
{
    public class WardrobeSearchResponseDto
    {
        public int WardrobeId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }
}
