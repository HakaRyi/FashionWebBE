using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Application.Request.TryOn
{
    public class CreateHistoryTryOnRequest
    {
        [Required]
        public IFormFile Image { get; set; } = null!;
    }
}
