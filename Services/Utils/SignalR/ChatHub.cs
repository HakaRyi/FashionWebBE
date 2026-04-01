using Microsoft.AspNetCore.SignalR;
using Services.Implements.Auth;
using Services.Implements.ChatImp;
using Services.Request.MessageReq;

namespace Services.Utils.SignalR
{
    public class ChatHub : Hub
    {
        private readonly IChatService _service;
        private readonly IGroupService _groupService;
        private readonly ICurrentUserService _currentUserService;
        private static readonly Dictionary<string, string> _connections = new();


        public ChatHub(IChatService service, ICurrentUserService userService, IGroupService groupService)
        {
            _service = service;
            _currentUserService = userService;
            _groupService = groupService;
        }
        public override async Task OnConnectedAsync()
        {
            var userId = _currentUserService.GetUserId() ?? 0;
            if (userId == 0) throw new HubException("Bạn cần đăng nhập để kết nối.");
            var userGroups = await _groupService.GetMyGroupList();
            foreach (var group in userGroups)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, group.GroupId.ToString());
            }
            await Clients.All.SendAsync("UserOnline", userId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception ex)
        {
            var userId = _currentUserService.GetUserId() ?? 0;
            if (userId == 0) throw new HubException("Bạn cần đăng nhập để kết nối.");
            await Clients.All.SendAsync("UserOffline", userId);
            await base.OnDisconnectedAsync(ex);
        }

        public async Task SendMessage(int groupId, SendMessageRequest request)
        {
            var userId = _currentUserService.GetUserId() ?? 0;
            if (userId == 0) throw new HubException("Bạn cần đăng nhập để kết nối.");
            await _service.SendMessage(groupId, request);
        }

        public async Task SendReaction(int groupId, int messageId, string type)
        {
            var userId = _currentUserService.GetUserId() ?? 0;
            if (userId == 0) throw new HubException("Bạn cần đăng nhập để kết nối.");
            await _service.AddReaction(messageId, type);
            await Clients.Group(groupId.ToString()).SendAsync("ReceiveReaction", messageId, userId, type);  // Broadcast
        }

        public async Task PinMessage(int messageId, int groupId)
        {
            await _service.PinMessage(messageId, groupId);
            await Clients.Group(groupId.ToString()).SendAsync("MessagePinned", messageId);
        }
        public async Task UnPinMessage(int PinMsgId, int groupId)
        {
            await _service.UnPinMessage(PinMsgId);
            await Clients.Group(groupId.ToString()).SendAsync("MessageUnPinned", PinMsgId);
        }

        public async Task DeleteMessage(int groupId, int messageId)
        {
            await _service.DeleteMessage(messageId);
            await Clients.Group(groupId.ToString()).SendAsync("MessageDeleted", messageId);
        }
        public async Task JoinGroup(int groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId.ToString());
        }
        public async Task RecallMessage(int groupId, int messageId)
        {
            await _service.RecallMessage(messageId);
            await Clients.Group(groupId.ToString()).SendAsync("MessageRecalled", messageId);
        }
        public async Task EditMessage(int groupId, int messageId, EditMessageRequest request)
        {
            await _service.UpdateMessage(messageId, request);
            await Clients.Group(groupId.ToString()).SendAsync("MessageEdited", messageId);
        }
        public async Task NotifyNewGroup(int groupId, List<int> memberIds)
        {
            foreach (var userId in memberIds)
            {
                await Clients.User(userId.ToString()).SendAsync("NewGroupCreated", groupId);
            }
        }
    }
}
