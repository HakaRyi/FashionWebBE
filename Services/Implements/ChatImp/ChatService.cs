using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Repositories.Entities;
using Repositories.Repos.AccountRepos;
using Repositories.Repos.ChatRepos;
using Repositories.Repos.GroupRepos;
using Repositories.UnitOfWork;
using Service.DTO.Request;
using Services.Implements.Auth;
using Services.Response.MessageResp;

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
        public ChatService(IUnitOfWork unitOfWork, IChatRepository chatrepo, 
            IGroupRepository groupRepository,
            ICurrentUserService userService,
            IAccountRepository accountRepository,
            IPhotoRepository photoRepository)
        {
            _unitOfWork = unitOfWork;
            _chatrepo = chatrepo;
            _groupRepository = groupRepository;
            _currentUserService = userService;
            _accountRepository = accountRepository;
            _photoRepository = photoRepository;
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

        public async Task SendMessage(int groupId,SendMessageRequest request)
        {
            int senderId = _currentUserService.GetUserId()??0;
            if (senderId == 0) throw new Exception("User not authenticated");
            var sender = await _accountRepository.GetAccountById(senderId);
            var message = new Message
            {
                Content = request.content,
                GroupId = groupId,
                AccountId = senderId,
                SentAt = DateTime.UtcNow,
                ReplyToMessageId = request.replyToId,
                IsRecalled = request.isRecalled
            };
            foreach (var url in request.photoUrls ?? new List<string>())
            {
                var photo = new Photo { PhotoUrl = url, MessageId = message.MessageId };
                await _photoRepository.AddPhotoAsync(photo);
            }
            await _chatrepo.AddMessage(message);
            await _unitOfWork.CommitAsync();
        }

        public async Task UpdateMessage(int messageId,EditMessageRequest request)
        {
            int senderId = _currentUserService.GetUserId()??0;
            if (senderId == 0) throw new Exception("User not authenticated");
            var message = await _chatrepo.GetMessageById(messageId);
            if(message.AccountId!= senderId) throw new Exception("User not authorized to edit this message");
            message.Content = request.newContent;
            message.SentAt = DateTime.UtcNow;
            await _chatrepo.EditMessage(message);
            await _unitOfWork.CommitAsync();
        }
        public async Task DeletePhotoFromMessageId(int messageId)
        {
            int senderId = _currentUserService.GetUserId()??0;
            if (senderId == 0) throw new Exception("User not authenticated");
            var message = await _chatrepo.GetMessageById(messageId);
            if(message.AccountId!= senderId) throw new Exception("User not authorized to delete photos from this message");
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
    }
}
