using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CloudinaryDotNet;
using Microsoft.EntityFrameworkCore;
using Repositories.Entities;
using Repositories.Repos.AccountRepos;
using Repositories.Repos.GroupRepos;
using Repositories.UnitOfWork;
using Services.Implements.Auth;
using Services.Request.GroupReq;
using Services.Response.GroupResp;

namespace Services.Implements.ChatImp
{
    public class GroupService : IGroupService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGroupRepository _groupRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAccountRepository _accountRepository;
        public GroupService(IUnitOfWork unitOfWork, IGroupRepository groupRepository, ICurrentUserService currentUserService, IAccountRepository accountRepository)
        {
            _unitOfWork = unitOfWork;
            _groupRepository = groupRepository;
            _currentUserService = currentUserService;
            _accountRepository = accountRepository;
        }

        public async Task CreateGroup(GroupRequest request)
        {
            var currentUserId = _currentUserService.GetUserId()??0;
            if (currentUserId == 0) throw new Exception("User not authenticated");
            var group = new Group
            {
                Name = request.Name,
                IsGroup = true,
                CreateBy = currentUserId,
                GroupUsers = new List<GroupUser>
                {
                    new GroupUser { AccountId = currentUserId, JoinedAt = DateTime.Now }
                }
            };
            await _groupRepository.CreateGroup(group);
            await _unitOfWork.CommitAsync();

        }
        public async Task AddMemberToGroup(int groupId, int userId)
        {
            var currentUserId = _currentUserService.GetUserId()??0;
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
            var account = await _groupRepository.GetAccountFromGroup(groupId,userId);
            if (account == null)
            {
                throw new Exception("User not found in group");
            }
            await _groupRepository.KickMemberFromGroup(account);
            await _unitOfWork.CommitAsync();
        }
        public async Task CreateGroup2User(int targetUserId)
        {
            var currentUserId =  _currentUserService.GetUserId()??0;
            if (currentUserId == 0) throw new Exception("User not authenticated");
            var isExisted = await _groupRepository.CheckIsRoomExist(currentUserId,targetUserId);
            if (isExisted) throw new Exception("You and this user have already private room");
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
            await _groupRepository.UpdateGroup(groupExingting);
            await _unitOfWork.CommitAsync();
        }

        public async Task<List<GroupResponse>> GetMyGroupList()
        {
            var currentUserId = _currentUserService.GetUserId()??0;
            if (currentUserId == 0) throw new Exception("User not authenticated");
            var groups = await _groupRepository.GetGroupsByAccountId(currentUserId);
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
    }
}
