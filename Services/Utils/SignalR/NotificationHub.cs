using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Services.Utils.SignalR
{
    [Authorize]
    public class NotificationHub : Hub
    {

    }
}
