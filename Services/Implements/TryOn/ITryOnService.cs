using Microsoft.AspNetCore.Http;
using Services.Request.TryOn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.TryOn
{
    public interface ITryOnService
    {
        Task<Stream> ProcessTryOnAsync(IFormFile modelImage, IFormFile clothImage);
    }
}
