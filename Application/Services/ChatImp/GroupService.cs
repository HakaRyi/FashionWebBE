using Application.Interfaces;
using Application.Request.GroupReq;
using Application.Response.AccountRep;
using Application.Response.GroupResp;
using Application.Utils;
using Application.Utils.SignalR;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.ChatImp
{
    public class GroupService : IGroupService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGroupRepository _groupRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAccountRepository _accountRepository;
        private readonly ICloudStorageService _storage;
        private readonly IHubContext<ChatHub> _hubContext;
        public GroupService(IUnitOfWork unitOfWork,
            IGroupRepository groupRepository,
            ICurrentUserService currentUserService,
            IAccountRepository accountRepository,
            ICloudStorageService storageService,
            IHubContext<ChatHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _groupRepository = groupRepository;
            _currentUserService = currentUserService;
            _accountRepository = accountRepository;
            _storage = storageService;
            _hubContext = hubContext;
        }

        public async Task CreateGroup(GroupRequest request)
        {
            var currentUserId = _currentUserService.GetUserId() ?? 0;
            if (currentUserId == 0) throw new Exception("User not authenticated");
            if (request.MemberIds == null || !request.MemberIds.Any())
            {
                throw new Exception("Cần ít nhất 2 thành viên để tạo nhóm.");
            }
            var groupUsers = new List<GroupUser>
            {
                new GroupUser { AccountId = currentUserId, JoinedAt = DateTime.Now }
            };
            foreach (var memberId in request.MemberIds.Distinct())
            {
                if (memberId != currentUserId) 
                {
                    groupUsers.Add(new GroupUser
                    {
                        AccountId = memberId,
                        JoinedAt = DateTime.Now
                    });
                }
            }
            var group = new Group
            {
                Name = string.IsNullOrWhiteSpace(request.Name) ? "Nhóm mới" : request.Name,
                IsGroup = true,
                CreateBy = currentUserId,
                CreatedAt = DateTime.Now,
                GroupUsers = groupUsers
            };
            if (request.Avatar != null)
            {
                var avatarUrl = await _storage.UploadImageAsync(request.Avatar);
                group.Images.Add(new Image
                {
                    ImageUrl = avatarUrl,
                    OwnerType = "Group",
                    CreatedAt = DateTime.Now
                });
            }
            await _groupRepository.CreateGroup(group);
            await _unitOfWork.CommitAsync();
            foreach (var memberId in request.MemberIds)
            {
                await _hubContext.Clients.User(memberId.ToString()).SendAsync("NewGroupCreated", group.GroupId);
            }
            await _hubContext.Clients.User(currentUserId.ToString()).SendAsync("NewGroupCreated", group.GroupId);

        }
        public async Task AddMemberToGroup(int groupId, int userId)
        {
            var currentUserId = _currentUserService.GetUserId() ?? 0;
            if (currentUserId == 0) throw new Exception("User not authenticated");
            //kiem tra th user dang dang nhap da trong group chua, neu chua thi khong the them nguoi khac vao group duoc
            var checkUser1IsInGroup = await _groupRepository.GetAccountFromGroup(groupId, currentUserId);
            if (checkUser1IsInGroup == null)
            {
                throw new Exception("You are not in this group");
            }
            //kiem tra user can them da trong group chua, neu roi thi khong the them nua
            var checkUser2IsInGroup = await _groupRepository.GetAccountFromGroup(groupId, userId);
            if (checkUser2IsInGroup != null)
            {
                throw new Exception("User already in group");
            }

            await _groupRepository.AddMemberToGroup(new GroupUser
            {
                AccountId = userId,
                GroupId = groupId,
                JoinedAt = DateTime.Now
            });
            await _unitOfWork.CommitAsync();
            await _hubContext.Clients.User(userId.ToString()).SendAsync("NewGroupCreated", groupId);
        }
        public async Task KickMemberToGroup(int groupId, int userId)
        {
            var account = await _groupRepository.GetAccountFromGroup(groupId, userId);
            if (account == null)
            {
                throw new Exception("User not found in group");
            }
            await _groupRepository.KickMemberFromGroup(account);
            await _unitOfWork.CommitAsync();
        }
        public async Task<int?> CheckExisting1v1Group(int targetUserId)
        {
            var currentUserId = _currentUserService.GetUserId() ?? 0;
            if (currentUserId == 0) throw new Exception("User not authenticated");

            var room = await _groupRepository.GetExisting1v1Room(currentUserId, targetUserId);
            return room?.GroupId;
        }
        public async Task<int> CreateGroup2User(int targetUserId)
        {
            var currentUserId = _currentUserService.GetUserId() ?? 0;
            if (currentUserId == 0) throw new Exception("User not authenticated");
            var existingRoom = await _groupRepository.GetExisting1v1Room(currentUserId, targetUserId);
            if (existingRoom!=null) return existingRoom.GroupId;
            var group = new Group
            {
                Name = "Private Chat",
                IsGroup = false,
                CreatedAt = DateTime.Now,
                GroupUsers = new List<GroupUser>
                {
                    new GroupUser { AccountId = currentUserId, JoinedAt = DateTime.Now },
                    new GroupUser { AccountId = targetUserId, JoinedAt = DateTime.Now }
                }
            };
            _groupRepository.CreateGroup(group);
            await _unitOfWork.CommitAsync();
            await _hubContext.Clients.User(currentUserId.ToString()).SendAsync("NewGroupCreated", group.GroupId);
            await _hubContext.Clients.User(targetUserId.ToString()).SendAsync("NewGroupCreated", group.GroupId);
            return group.GroupId;
        }


        public async Task DeleteGroup(int groupId)
        {
            var group = await _groupRepository.GetGroupById(groupId);
            if (group == null || group.IsGroup == false)
            {
                throw new Exception("Group not found or this is not group");
            }
            _groupRepository.DeleteGroup(group);
            await _unitOfWork.CommitAsync();
        }

        public async Task<GroupResponse> GetGroupById(int groupId)
        {
            var group = await _groupRepository.GetGroupById(groupId);
            if (group == null)
            {
                throw new Exception("Group not found");
            }
            return new GroupResponse
            {
                GroupId = group.GroupId,
                Name = group.Name,
                IsGroup = group.IsGroup,
                CreateBy = group.GroupUsers
                        .FirstOrDefault(gu => gu.AccountId == group.CreateBy)?
                        .Account?.UserName ?? "Unknown",
                CreatedAt = group.CreatedAt
            };
        }

        public async Task<List<GroupResponse>> GetGroupsByAccountId(int accountId)
        {
            var groups = await _groupRepository.GetGroupsByAccountId(accountId);
            return groups.Select(group => new GroupResponse
            {
                GroupId = group.GroupId,
                Name = group.Name,
                IsGroup = group.IsGroup,
                CreateBy = group.GroupUsers
                        .FirstOrDefault(gu => gu.AccountId == group.CreateBy)?
                        .Account?.UserName ?? "Unknown",
                CreatedAt = group.CreatedAt
            }).ToList();
        }

        public async Task UpdateGroup(int groupId, EditGroupRequest request)
        {
            var groupExingting = await _groupRepository.GetGroupById(groupId);
            if (groupExingting == null)
            {
                throw new Exception("Group not found");
            }
            groupExingting.Name = request.Name;
            if (request.Avatar != null)
            {
                var avatarUrl = await _storage.UploadImageAsync(request.Avatar);
                // Thêm ảnh mới vào bảng Image cho Group
                groupExingting.Images.Add(new Image
                {
                    ImageUrl = avatarUrl,
                    OwnerType = "Group",
                    CreatedAt = DateTime.Now
                });
            }
            await _groupRepository.UpdateGroup(groupExingting);
            await _unitOfWork.CommitAsync();
        }

        public async Task<List<GroupResponse>> GetMyGroupList()
        {
            var currentUserId = _currentUserService.GetUserId() ?? 0;
            if (currentUserId == 0) throw new Exception("User not authenticated");
            var groups = await _groupRepository.GetGroupsByAccountId(currentUserId);
            return groups.Select(group =>
            {
                var lastMsg = group.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault();
                var response = new GroupResponse
                {
                    GroupId = group.GroupId,
                    IsGroup = group.IsGroup,
                    CreatedAt = group.CreatedAt,
                    LastMessage = lastMsg?.IsRecalled == true ? "Tin nhắn đã bị thu hồi" : lastMsg?.Content ?? "Tap to start chatting",
                    LastMessageAt = lastMsg?.SentAt ?? group.CreatedAt
                };

                if (group.IsGroup == false)
                {
                    var otherUser = group.GroupUsers.FirstOrDefault(gu => gu.AccountId != currentUserId)?.Account;
                    response.Name = otherUser?.UserName ?? "Người dùng hệ thống";
                    response.IsOnline = otherUser?.IsOnline;
                    response.Avatar = otherUser?.Avatars
                        .OrderByDescending(img => img.CreatedAt)
                        .FirstOrDefault()?.ImageUrl;
                    response.OtherUserId = otherUser?.Id ?? 0;
                }
                else
                {
                    response.Name = group.Name;
                    response.IsOnline = "Online";
                    response.Avatar = group.Images?
                        .OrderByDescending(img => img.CreatedAt)
                        .FirstOrDefault()?.ImageUrl ?? "https://static.vecteezy.com/system/resources/previews/009/292/244/non_2x/default-avatar-icon-of-social-media-user-vector.jpg";
                }
                return response;
            }).OrderByDescending(r => r.LastMessageAt)
            .ToList();
        }

        public async Task<List<UserInGroupResponse>> GetUsersInGroup(int groupId)
        {
            var groupUsers = await _groupRepository.GetUsersInGroup(groupId);
            return groupUsers.Select(gu => new UserInGroupResponse
            {
                Id = gu.AccountId,
                Username = gu.Account?.UserName ?? "Unknown",
                Avatar = gu.Account?.Avatars
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefault()?.ImageUrl,
                Status = gu.Account?.Status ?? "Unknown",
                IsOnline = gu.Account?.IsOnline
            })
                .ToList();
        }

        public async Task<List<PhotoInGroupResponse>> GetPhotos(int groupId)
        {
            var photos = await _groupRepository.GetPhotosInGroup(groupId);
            return photos.Select(p => new PhotoInGroupResponse
            {
                PhotoId = p.PhotoId,
                Url = p.PhotoUrl,
                GroupId = p.Message.GroupId,
                AccountId = p.Message.Account.Id,
                AccountName = p.Message.Account?.UserName ?? "Unknown",
                AccountAvatar = p.Message.Account?.Avatars
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefault()?.ImageUrl,
                CreatedAt = p.Message.SentAt,
            }).ToList();
        }
    }
}
