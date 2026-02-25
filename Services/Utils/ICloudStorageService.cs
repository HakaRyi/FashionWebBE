using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Utils
{
    public interface ICloudStorageService
    {
        Task<string> UploadImageAsync(IFormFile file);
        Task<string> UploadImageFromStreamAsync(Stream stream, string fileName);
    }
}
