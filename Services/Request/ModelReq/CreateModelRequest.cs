using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Services.Request.ModelReq
{
    public class CreateModelRequest
    {
        [Required]
        public IFormFile Image { get; set; } = null!;
    }
}
