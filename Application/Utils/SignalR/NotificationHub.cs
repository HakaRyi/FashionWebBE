using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Application.Utils.SignalR
{
    [Authorize]
    public class NotificationHub : Hub
    {

    }
}
