using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Request.TryOn
{
    public class CreateHistoryTryOnRequest
    {
        [Required]
        public IFormFile Image { get; set; } = null!;
    }
}
