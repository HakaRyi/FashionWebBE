using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.Auth
{
    public interface ICurrentUserService
    {
        int? GetUserId();
        int GetRequiredUserId();
        string? GetEmail();
        bool IsAuthenticated();
    }
}
