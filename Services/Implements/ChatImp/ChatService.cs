using Microsoft.AspNetCore.SignalR;
using Repositories.Entities;
using Repositories.Repos.AccountRepos;
using Repositories.Repos.ChatRepos;
using Repositories.Repos.GroupRepos;
using Repositories.UnitOfWork;
using Services.Implements.Auth;
using Services.RabbitMQ;
using Services.Request.MessageReq;
using Services.Response.MessageResp;
using Services.Response.MessReactResp;
using Services.Utils;
using Services.Utils.SignalR;

namespace Services.Implements.ChatImp
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IChatRepository _chatrepo;
        private readonly IGroupRepository _groupRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPhotoRepository _photoRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IPinMessageRepository _pinMessageRepository;
        private readonly ICloudStorageService _storage;
        private readonly IRabbitMQProducer _rabbitMQProducer;
        private readonly IHubContext<ChatHub> _hubContext;
        public ChatService(IUnitOfWork unitOfWork,
            IChatRepository chatrepo,
            IGroupRepository groupRepository,
            ICurrentUserService userService,
            IAccountRepository accountRepository,
            IPhotoRepository photoRepository,
            IPinMessageRepository pinMessageRepository,
            ICloudStorageService storage,
            IRabbitMQProducer rabbitMQProducer,
            IHubContext<ChatHub> hubContext
            )
        {
            _unitOfWork = unitOfWork;
            _chatrepo = chatrepo;
            _groupRepository = groupRepository;
            _currentUserService = userService;
            _accountRepository = accountRepository;
            _photoRepository = photoRepository;
            _pinMessageRepository = pinMessageRepository;
            _storage = storage;
            _rabbitMQProducer = rabbitMQProducer;
            _hubContext = hubContext;
        }

        public async Task DeleteMessage(int messageId)
        {
            try
            {
                var message = await _chatrepo.GetMessageById(messageId);
                await _chatrepo.DeleteMessage(message);
                await _unitOfWork.CommitAsync();

            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
            }
        }

        public async Task<List<MessageResponse>> GetHistoryMessage(int groupId)
        {
            var messages = await _chatrepo.GetHistoryMessages(groupId);
            var group = await _groupRepository.GetGroupById(groupId);
            return messages.Select(m => new MessageResponse
            {
                MessageId = m.MessageId,
                GroupName = m.Group.Name,
                SenderName = m.Account.UserName,
                SenderId = m.Account.Id,
                Content = m.Content,
                SentAt = m.SentAt,
                Photos = m.Photos.Select(p => p.PhotoUrl).ToList(),
                Reactions = m.MessReactions.Select(r => new MessageReactionResponse
                {
                    AccountId = r.AccountReactId,
                    ReactionType = r.Type
                }).ToList(),
                ReplyToMessageId = m.ReplyToMessageId
            }).ToList();


        }

        public async Task<MessageResponse> GetMessageById(int messageId)
        {
            var message = await _chatrepo.GetMessageById(messageId);
            return new MessageResponse()
            {
                MessageId = message.MessageId,
                GroupName = message.Group.Name,
                SenderName = message.Account.UserName,
                SenderId = message.Account.Id,
                Content = message.Content,
                SentAt = message.SentAt,
                Photos = message.Photos.Select(p => p.PhotoUrl).ToList(),
                Reactions = message.MessReactions.Select(r => new MessageReactionResponse
                {
                    AccountId = r.AccountReactId,
                    ReactionType = r.Type
                }).ToList(),
                ReplyToMessageId = message.ReplyToMessageId
            };


        }

        public async Task SendMessage(int groupId, SendMessageRequest request)
        {
            int senderId = _currentUserService.GetUserId() ?? 0;
            if (senderId == 0) throw new Exception("User not authenticated");
            var sender = await _accountRepository.GetAccountById(senderId);
            var group = await _groupRepository.GetGroupById(groupId);
            List<string> imageUrls = new List<string>();
            if (request.photo != null && request.photo.Any())
            {
                var uploadTasks = request.photo.Select(f => _storage.UploadImageAsync(f));
                imageUrls = (await Task.WhenAll(uploadTasks)).ToList();
            }
            var queueMessage = new ChatMessageQueueDto
            {
                GroupId = groupId,
                SenderId = senderId,
                Content = request.content,
                ImageUrls = imageUrls,
                ReplyToId = request.replyToId
            };
            await _rabbitMQProducer.SendMessage(queueMessage);

        }

        public async Task UpdateMessage(int messageId, EditMessageRequest request)
        {
            int senderId = _currentUserService.GetUserId() ?? 0;
            if (senderId == 0) throw new Exception("User not authenticated");
            var message = await _chatrepo.GetMessageById(messageId);
            if (message.AccountId != senderId) throw new Exception("User not authorized to edit this message");
            message.Content = request.newContent;
            message.SentAt = DateTime.UtcNow;
            await _chatrepo.EditMessage(message);
            await _unitOfWork.CommitAsync();
        }
        public async Task DeletePhotoFromMessageId(int messageId)
        {
            int senderId = _currentUserService.GetUserId() ?? 0;
            if (senderId == 0) throw new Exception("User not authenticated");
            var message = await _chatrepo.GetMessageById(messageId);
            if (message.AccountId != senderId) throw new Exception("User not authorized to delete photos from this message");
            var photo = await _photoRepository.GetPhotoFromMessageId(messageId);
            if (photo != null)
            {
                foreach (var p in photo)
                {
                    await _photoRepository.DeletePhotoAsync(p);
                }
                await _unitOfWork.CommitAsync();
            }
        }
        public async Task RecallMessage(int messageId)
        {
            var userId = _currentUserService.GetUserId() ?? 0;
            var message = await _chatrepo.GetMessageById(messageId);

            if (message == null) throw new Exception("Tin nhắn không tồn tại");
            if (message.AccountId != userId) throw new Exception("Bạn không thể thu hồi tin nhắn của người khác");

            message.Content = "Tin nhắn đã bị thu hồi";
            message.IsRecalled = true;

            await _chatrepo.EditMessage(message);
            await _unitOfWork.CommitAsync();
        }
        public async Task AddReaction(int messageId, string type)
        {
            var userId = _currentUserService.GetUserId() ?? 0;
            var reaction = new MessReaction
            {
                MessageId = messageId,
                AccountReactId = userId,
                Type = type
            };
            await _chatrepo.AddOrUpdateReaction(reaction);
            await _unitOfWork.CommitAsync();
            var message = await _chatrepo.GetMessageById(messageId);
            await _hubContext.Clients.Group(message.GroupId.ToString()).SendAsync("ReactionUpdated", messageId, type);
        }

        public async Task PinMessage(int messageId, int groupId)
        {
            var userId = _currentUserService.GetUserId() ?? 0;
            if (userId == 0) throw new Exception("User not authenticated");
            var pinMessage = new PinnedMessage
            {
                MessageId = messageId,
                GroupId = groupId,
                AccountPinnedId = userId,
                PinnedAt = DateTime.UtcNow
            };
            await _pinMessageRepository.AddPinnedMessageAsync(pinMessage);
            await _unitOfWork.CommitAsync();

        }
        public async Task UnPinMessage(int pinMsg)
        {
            var pinMessage = await _pinMessageRepository.GetPinnedMessageAsync(pinMsg);
            await _pinMessageRepository.RemovePinnedMessageAsync(pinMessage);
            await _unitOfWork.CommitAsync();

        }

        public async Task<List<PinMessageResponse>> GetPinnedMessagesByGroupId(int groupId)
        {
            var pinnedMessages = await _pinMessageRepository.GetPinnedMessagesByGroupIdAsync(groupId);
            return pinnedMessages.Select(pm => new PinMessageResponse
            {
                PinnedMsgId = pm.PinnedMsgId,
                MessageId = pm.MessageId,
                GroupId = pm.GroupId,
                AccountPinnedId = pm.AccountPinnedId,
                PinnedAt = pm.PinnedAt,
                AccountPinnedName = pm.AccountPinned.UserName,
                MessageContent = pm.Message.Content,
                MessagePhotos = pm.Message.Photos.Select(p => p.PhotoUrl).ToList()
            }).ToList();
        }
        public async Task<List<MessReactResponse>> GetReactorByMessId(int messId)
        {
            var reactions = await _chatrepo.GetAllReactionByMessageiD(messId);
            return reactions.Select(r => new MessReactResponse
            {
                ReactId = r.ReactId,
                AccountId = r.AccountReactId ?? 0,
                AccountName = r.AccountReact.UserName,
                ReactType = r.Type,
                MessageId = r.MessageId ?? 0
            }).ToList() ?? null;
        }


    }
}
