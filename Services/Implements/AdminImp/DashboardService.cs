using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repositories.Repos.AdminRepos;
using Repositories.Repos.Events;
using Repositories.UnitOfWork;
using Services.Request.AdminReq;
using Services.Request.NotificationReq;
using Services.Response.AccountRep;
using Services.Response.AdminResp;
using Services.Response.EventResp;
using Services.Response.TransactionResp;
namespace Services.Implements.AdminImp
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<Repositories.Entities.Account> _userManager;
        private readonly IEventRepository _eventRepository;
        public DashboardService(IDashboardRepository dashboardRepository, 
            IUnitOfWork unitOfWork, 
            UserManager<Repositories.Entities.Account> userManager,
            IEventRepository eventRepository)
        {
            _dashboardRepository = dashboardRepository;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _eventRepository = eventRepository;
        }

        public async Task AdminCheckEvent(int eventId,AdminCheckRequest request)
        {
            var entity = await _eventRepository.GetByIdAsync(eventId);
            if (entity == null) throw new Exception("ko thay su kien");
            entity.Status = request.TheStatus;
            _eventRepository.Update(entity);
            await _unitOfWork.CommitAsync();
        }

        public async Task<List<AccountResponse>> Get3NewestUser()
        {
            var users = await _dashboardRepository.Get3NewestUser();
            var responses = new List<AccountResponse>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                responses.Add(new AccountResponse
                {
                    Id = user.Id,
                    Username = user.UserName,
                    Email = user.Email,
                    Avatar = user.Avatars
                      .OrderByDescending(img => img.CreatedAt)
                      .Select(img => img.ImageUrl)
                      .FirstOrDefault() ?? null,
                    Role = roles.FirstOrDefault() ?? "User",
                    CreatedAt = user.CreatedAt,
                    Status = user.Status,
                    FollowerCount = user.CountFollower,
                    FollowingCount = user.CountFollowing,
                    PostCount = user.CountPost,
                    Description = user.Description,
                    IsOnline = user.IsOnline
                });
            }
            return responses;

        }

        public async Task<PagedNotificationResponse> GetAdminNotifications(int pageIndex, int pageSize)
        {
            var query = _dashboardRepository.GetAdminNotificationsQuery();

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationDto
                {
                    NotificationId = n.NotificationId,
                    Title = n.Title,
                    Content = n.Content,
                    Type = n.Type,
                    SenderName = n.Sender.UserName,
                    SenderAvatar = n.Sender.Avatars
                      .OrderByDescending(img => img.CreatedAt)
                      .Select(img => img.ImageUrl)
                      .FirstOrDefault() ?? null,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return new PagedNotificationResponse { Items = items, TotalCount = totalCount };
        }

        public async Task<DashboardViewDto> GetDashboardInformation(DashboardRequest request)
        {
            var start = request.StartDate ?? DateTime.Now.AddDays(-7);
            var end = request.EndDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.Now;

            var overview = new OverviewDto
            {
                TotalRevenue = await _dashboardRepository.GetRevenueTransactions().SumAsync(t => t.Amount),
                TotalUsers = await _dashboardRepository.GetAccountsByRole(2).CountAsync(),
                TotalExperts = await _dashboardRepository.GetAccountsByRole(3).CountAsync(),
                TotalPosts = await _dashboardRepository.GetPosts().CountAsync()
            };
            var revenueData = await _dashboardRepository.GetRevenueTransactions()
                .Where(t => t.CreatedAt >= start && t.CreatedAt <= end)
                .GroupBy(t => t.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Value = g.Sum(t => t.Amount) })
                .ToListAsync();

            var userData = await _dashboardRepository.GetAccountsByRole(2)
                .Where(a => a.CreatedAt >= start && a.CreatedAt <= end)
                .GroupBy(a => a.CreatedAt.Value.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var expertData = await _dashboardRepository.GetAccountsByRole(3)
                .Where(a => a.CreatedAt >= start && a.CreatedAt <= end)
                .GroupBy(a => a.CreatedAt.Value.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var postData = await _dashboardRepository.GetPosts()
                .Where(p => p.CreatedAt >= start && p.CreatedAt <= end)
                .GroupBy(p => p.CreatedAt.Value.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            List<ChartPointDto> FormatData(IEnumerable<dynamic> rawData, bool isMoney = false)
            {
                return rawData.Select(x => new ChartPointDto
                {
                    Name = ((DateTime)x.Date).ToString("dd/MM"),
                    Value = isMoney ? (decimal)x.Value : (decimal)x.Count
                }).OrderBy(x => x.Name).ToList();
            }

            return new DashboardViewDto
            {
                Overview = overview,
                RevenueChart = FormatData(revenueData, true),
                UserChart = FormatData(userData),
                ExpertChart = FormatData(expertData),
                PostChart = FormatData(postData)
            };
        }

        public async Task<PagedAdminEventResponse> GetEvents(int pageIndex, int pageSize)
        {
            var query = _dashboardRepository.GetEvents();

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new AdminEventResponse
                {
                    EventId = n.EventId,
                    CreatorName = n.Creator.UserName,
                    Title = n.Title,
                    Description = n.Description,
                    Status = n.Status,
                    StartTime = n.StartTime,
                    EndTime = n.EndTime,
                    AppliedFee = n.AppliedFee,
                    CreatorId = n.CreatorId,
                    ExpertWeight = n.ExpertWeight,
                    UserWeight = n.UserWeight,
                    Prizes = n.PrizeEvents.Select(p => new PrizeDtoV1
                    {
                        PrizeEventId = p.PrizeEventId,
                        Ranked = p.Ranked,
                        RewardAmount = p.RewardAmount,
                        Status = p.Status
                    }).OrderBy(p => p.Ranked).ToList(),
                    Experts = n.EventExperts.Select(ex => new ExpertInEventDto
                    {
                        ExpertId = ex.ExpertId,
                        FullName = ex.Expert.UserName,
                    }).ToList(),
                })
                .ToListAsync();

            return new PagedAdminEventResponse { Items = items, TotalCount = totalCount };
        }

        public async Task<List<TransactionResponse>> GetTransactionList(DashboardRequest request)
        {
            var start = request.StartDate ?? DateTime.Now.AddDays(-7);
            var end = request.EndDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.Now;
            var transactions = await _dashboardRepository.GetRevenueTransactions()
                .Include(t => t.Wallet)            // Nạp bảng Wallet
                    .ThenInclude(w => w.Account)
                .Where(a => a.CreatedAt >= start && a.CreatedAt <= end)
                .ToListAsync();
            var responses = new List<TransactionResponse>();

            foreach (var tran in transactions)
            {
                responses.Add(new TransactionResponse
                {
                    Amount = tran.Amount,
                    BalanceAfter = tran.BalanceAfter,
                    BalanceBefore = tran.BalanceBefore,
                    CreatedAt = tran.CreatedAt,
                    Description = tran.Description,
                    PaymentId = tran.PaymentId,
                    ReferenceId = tran.ReferenceId,
                    ReferenceType = tran.ReferenceType,
                    Status = tran.Status,
                    TransactionId = tran.TransactionId,
                    Type = tran.Type,
                    UserName = tran.Wallet.Account.UserName,
                    WalletId = tran.WalletId




                });
            }
            return responses;

        }
    }
}
