using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Utils.File
{
    public interface IFileService
    {
        Task<string> UploadAsync(IFormFile file);

        //Task<bool> DeleteAsync(string publicId);
    }
}
