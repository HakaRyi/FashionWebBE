using Application.Interfaces;
using Application.Request.PostReq;
using Application.Response.MessageResp;
using Application.Services.PostImp;
using Application.Utils.SignalR;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Application.Services.ChatImp
{
    public class ChatShareService : IChatShareService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IChatRepository _chatRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly IPostRepository _postRepository;
        private readonly IFollowRepository _followRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAccountRepository _accountRepository;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatShareService(
            IUnitOfWork unitOfWork,
            IChatRepository chatRepository,
            IGroupRepository groupRepository,
            IPostRepository postRepository,
            IFollowRepository followRepository,
            ICurrentUserService currentUserService,
            IAccountRepository accountRepository,
            IHubContext<ChatHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _chatRepository = chatRepository;
            _groupRepository = groupRepository;
            _postRepository = postRepository;
            _followRepository = followRepository;
            _currentUserService = currentUserService;
            _accountRepository = accountRepository;
            _hubContext = hubContext;
        }

        public async Task SharePostAsync(SharePostToChatRequest request)
        {
            int senderId = _currentUserService.GetUserId() ?? 0;
            if (senderId == 0)
            {
                throw new Exception("User not authenticated");
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.PostId <= 0)
            {
                throw new Exception("Post id is invalid.");
            }

            var receiverIds = request.ReceiverAccountIds
                .Where(x => x > 0 && x != senderId)
                .Distinct()
                .ToList();

            if (!receiverIds.Any())
            {
                throw new Exception("Please choose at least one receiver.");
            }

            var sender = await _accountRepository.GetAccountById(senderId);
            if (sender == null)
            {
                throw new Exception("Sender not found.");
            }

            var post = await _postRepository.GetPostForShareAsync(request.PostId);
            if (post == null)
            {
                throw new Exception("Post not found.");
            }

            bool canSharePost = post.AccountId == senderId
                || (post.Status == PostStatus.Published
                    && post.Visibility == PostVisibility.Visible);

            if (!canSharePost)
            {
                throw new Exception("This post cannot be shared.");
            }

            var allowedReceiverIds = await _followRepository.GetShareableUserIdsAsync(senderId);

            foreach (var receiverId in receiverIds)
            {
                if (!allowedReceiverIds.Contains(receiverId))
                {
                    throw new Exception($"Receiver {receiverId} is not allowed.");
                }
            }

            var dispatchList = new List<ShareDispatchItem>();

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                foreach (var receiverId in receiverIds)
                {
                    var room = await _groupRepository.GetExisting1v1Room(senderId, receiverId);
                    bool isNewRoom = false;

                    if (room == null)
                    {
                        room = new Group
                        {
                            IsGroup = false
                        };

                        await _groupRepository.CreateGroup(room);

                        await _groupRepository.AddMemberToGroup(new GroupUser
                        {
                            Group = room,
                            AccountId = senderId,
                            JoinedAt = DateTime.UtcNow
                        });

                        await _groupRepository.AddMemberToGroup(new GroupUser
                        {
                            Group = room,
                            AccountId = receiverId,
                            JoinedAt = DateTime.UtcNow
                        });

                        isNewRoom = true;
                    }

                    var message = new Message
                    {
                        AccountId = senderId,
                        Group = room,
                        Content = request.Caption,
                        SharedPostId = post.PostId,
                        SentAt = DateTime.UtcNow,
                        IsRecalled = false
                    };

                    await _chatRepository.AddMessage(message);

                    dispatchList.Add(new ShareDispatchItem
                    {
                        ReceiverId = receiverId,
                        Group = room,
                        Message = message,
                        IsNewRoom = isNewRoom
                    });
                }

                post.ShareCount = (post.ShareCount ?? 0) + receiverIds.Count;
                _postRepository.Update(post);

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            foreach (var item in dispatchList)
            {
                if (item.IsNewRoom)
                {
                    await _hubContext.Clients.User(senderId.ToString())
                        .SendAsync("NewGroupCreated", item.Group.GroupId);

                    await _hubContext.Clients.User(item.ReceiverId.ToString())
                        .SendAsync("NewGroupCreated", item.Group.GroupId);
                }

                var response = await BuildMessageResponse(item.Message.MessageId);

                await _hubContext.Clients.Group(item.Group.GroupId.ToString())
                    .SendAsync("ReceiveMessage", response);
            }
        }

        private async Task<MessageResponse> BuildMessageResponse(int messageId)
        {
            var message = await _chatRepository.GetMessageById(messageId);

            return new MessageResponse
            {
                MessageId = message.MessageId,
                GroupId = message.GroupId ?? 0,
                GroupName = message.Group?.Name,
                SenderName = message.Account?.UserName,
                SenderAvatar = message.Account?.Avatars
                    .OrderByDescending(img => img.CreatedAt)
                    .Select(img => img.ImageUrl)
                    .FirstOrDefault(),
                SenderId = message.Account?.Id ?? 0,
                Content = message.Content,
                SentAt = message.SentAt,
                Photos = message.Photos.Select(p => p.PhotoUrl).ToList(),
                ReplyToMessageId = message.ReplyToMessageId,

                SharedPostId = message.SharedPostId,
                SharedPostTitle = message.SharedPost?.Title,
                SharedPostContent = message.SharedPost?.Content,
                SharedPostImages = message.SharedPost?.Images
                    .OrderBy(i => i.CreatedAt)
                    .Select(i => i.ImageUrl)
                    .ToList() ?? new List<string>(),
                SharedPostOwnerId = message.SharedPost?.AccountId ?? 0,
                SharedPostOwnerName = message.SharedPost?.Account?.UserName
            };
        }

        private class ShareDispatchItem
        {
            public int ReceiverId { get; set; }

            public Group Group { get; set; } = null!;

            public Message Message { get; set; } = null!;

            public bool IsNewRoom { get; set; }
        }
    }
}