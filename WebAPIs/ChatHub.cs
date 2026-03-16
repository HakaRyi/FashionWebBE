//using System.Security.Claims;
//using Microsoft.AspNetCore.SignalR;
//using Repo.Models;
//using Service;
//using Services.Implements.ChatImp;

//namespace ChatDemoApi
//{
//    public class ChatHub : Hub
//    {
//        private readonly ChatService _service;
//        private static readonly Dictionary<string, string> _connections = new();


//        public ChatHub(ChatService service)
//        {
//            _service = service;
//        }

//        public override async Task OnConnectedAsync()
//        {
//            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
//                 ?? Context.User.FindFirst("AccountID")?.Value;

//            if (string.IsNullOrEmpty(userId))
//            {
//                throw new HubException("Bạn cần đăng nhập để gửi tin nhắn.");
//            }
//            if (userId != null)
//            {
//                _connections[userId] = Context.ConnectionId;
//                await _service.UpdateUserOnline(userId, true);
//                await Clients.All.SendAsync("UserOnline", userId);
//            }
//            await base.OnConnectedAsync();
//        }

//        public override async Task OnDisconnectedAsync(Exception ex)
//        {
//            var userId = Context.User.FindFirst("AccountID")?.Value;
//            if (userId != null)
//            {
//                _connections.Remove(userId);
//                await _service.UpdateUserOnline(userId, false);
//                await Clients.All.SendAsync("UserOffline", userId);
//            }
//            await base.OnDisconnectedAsync(ex);
//        }

//        public async Task SendMessage(int groupId, string content, List<string> photoUrls, int? replyToId)
//        {
//            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
//                 ?? Context.User.FindFirst("AccountID")?.Value;

//            if (string.IsNullOrEmpty(userId))
//            {
//                throw new HubException("Bạn cần đăng nhập để gửi tin nhắn.");
//            }
//            var message = await _service.SendMessage(groupId, userId, content, photoUrls, replyToId);
//            await Clients.Group(groupId.ToString()).SendAsync("ReceiveMessage", message);
//        }

//        public async Task AddReaction(int messageId, string type)
//        {
//            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
//                 ?? Context.User.FindFirst("AccountID")?.Value;

//            if (string.IsNullOrEmpty(userId))
//            {
//                throw new HubException("Bạn cần đăng nhập để add reaction ");
//            }
//            await _service.AddReaction(messageId, userId, type);
//            await Clients.All.SendAsync("ReceiveReaction", messageId, type, userId);  // Broadcast
//        }

//        public async Task PinMessage(int messageId, int groupId)
//        {
//            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
//                 ?? Context.User.FindFirst("AccountID")?.Value;

//            if (string.IsNullOrEmpty(userId))
//            {
//                throw new HubException("Bạn cần đăng nhập để pin tin nhắn.");
//            }
//            await _service.PinMessage(messageId, groupId,userId);
//            await Clients.Group(groupId.ToString()).SendAsync("MessagePinned", messageId);
//        }

//        public async Task DeleteMessage(int messageId)
//        {
//            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
//                 ?? Context.User.FindFirst("AccountID")?.Value;

//            if (string.IsNullOrEmpty(userId))
//            {
//                throw new HubException("Bạn cần đăng nhập để xoa tin nhắn.");
//            }
//            await _service.DeleteMessage(messageId, userId);
//            await Clients.All.SendAsync("MessageDeleted", messageId);
//        }

//        // Tương tự cho Recall, Edit, JoinGroup (Clients.Group.AddToGroupAsync)
//        public async Task JoinGroup(int groupId)
//        {
//            await Groups.AddToGroupAsync(Context.ConnectionId, groupId.ToString());
//        }
//        public async Task RecallMessage(int messageId)
//        {
//            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
//                 ?? Context.User.FindFirst("AccountID")?.Value;

//            if (string.IsNullOrEmpty(userId))
//            {
//                throw new HubException("Bạn cần đăng nhập để recall tin nhắn.");
//            }
//            await _service.RecallMessage(messageId, userId);
//            await Clients.All.SendAsync("MessageRecalled", messageId);
//        }
//        public async Task EditMessage(int messageId, string newContent)
//        {
//            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
//                 ?? Context.User.FindFirst("AccountID")?.Value;

//            if (string.IsNullOrEmpty(userId))
//            {
//                throw new HubException("Bạn cần đăng nhập để edit tin nhắn.");
//            }
//            await _service.EditMessage(messageId, userId, newContent);
//            await Clients.All.SendAsync("MessageEdited", messageId);
//        }

//    }
//}
