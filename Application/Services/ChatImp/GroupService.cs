using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Application.Request.GroupReq;
using Application.Response.GroupResp;
using Application.Utils;
using Domain.Interfaces;

namespace Application.Services.ChatImp
{
    public class GroupService : IGroupService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGroupRepository _groupRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAccountRepository _accountRepository;
        private readonly ICloudStorageService _storage;
        public GroupService(IUnitOfWork unitOfWork,
            IGroupRepository groupRepository,
            ICurrentUserService currentUserService,
            IAccountRepository accountRepository,
            ICloudStorageService storageService)
        {
            _unitOfWork = unitOfWork;
            _groupRepository = groupRepository;
            _currentUserService = currentUserService;
            _accountRepository = accountRepository;
            _storage = storageService;
        }

        public async Task CreateGroup(GroupRequest request)
        {
            var currentUserId = _currentUserService.GetUserId() ?? 0;
            if (currentUserId == 0) throw new Exception("User not authenticated");
            var group = new Group
            {
                Name = request.Name,
                IsGroup = true,
                CreateBy = currentUserId,
                CreatedAt = DateTime.UtcNow,
                GroupUsers = new List<GroupUser>
                {
                    new GroupUser { AccountId = currentUserId, JoinedAt = DateTime.Now }
                }
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
                    LastMessage = lastMsg?.IsRecalled == true ? "Tin nhắn đã bị thu hồi" : lastMsg?.Content ?? "Bấm để trò chuyện",
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
                }
                else
                {
                    response.Name = group.Name;
                    response.IsOnline = "Online";
                    response.Avatar = group.Images?
                        .OrderByDescending(img => img.CreatedAt)
                        .FirstOrDefault()?.ImageUrl ?? "https://cdn-icons-png.flaticon.com/512/8377/8377384.png";
                }
                return response;
            }).OrderByDescending(r => r.LastMessageAt)
            .ToList();
        }
        
    }
}
