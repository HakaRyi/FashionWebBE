using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Utils
{
    public interface IAIDetectionService
    {
        Task<bool> DetectFashionItemsAsync(string imageUrl);
    }
}
