using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Application.Request.ModelReq
{
    public class CreateModelRequest
    {
        [Required]
        public IFormFile Image { get; set; } = null!;
    }
}
