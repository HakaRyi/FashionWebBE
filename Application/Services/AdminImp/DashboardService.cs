using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces;
using Application.Request.AdminReq;
using Application.Request.NotificationReq;
using Application.Response.AccountRep;
using Application.Response.AdminResp;
using Application.Response.EventResp;
using Application.Response.TransactionResp;
using Domain.Interfaces;
using Application.Request.AccountReq;

namespace Application.Services.AdminImp
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<Domain.Entities.Account> _userManager;
        private readonly IEventRepository _eventRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ITryOnHistoryRepository _tryOnHistoryRepository;
        private readonly IRecommendationHistoryRepository _recommendationHistoryRepository;
        public DashboardService(IDashboardRepository dashboardRepository,
            IUnitOfWork unitOfWork,
            UserManager<Domain.Entities.Account> userManager,
            IEventRepository eventRepository,
            IAccountRepository accountRepository,
            ICurrentUserService currentUserService,
            ITransactionRepository transactionRepository,
            ITryOnHistoryRepository tryOnHistoryRepository,
            IRecommendationHistoryRepository recommendationHistoryRepository
            )
        {
            _dashboardRepository = dashboardRepository;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _eventRepository = eventRepository;
            _accountRepository = accountRepository;
            _currentUserService = currentUserService;
            _transactionRepository = transactionRepository;
            _tryOnHistoryRepository = tryOnHistoryRepository;
            _recommendationHistoryRepository = recommendationHistoryRepository;
        }

        public async Task AdminCheckEvent(int eventId, AdminCheckRequest request)
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

            var transactions = await _transactionRepository.GetRevenueTransactionsWithDetailsAsync(start, end);

            var responses = new List<TransactionResponse>();
            if (!transactions.Any()) return responses;

            var eventIds = transactions.Where(t => t.ReferenceType == "Event" && t.ReferenceId.HasValue).Select(t => t.ReferenceId!.Value).Distinct().ToList();
            var tryOnIds = transactions.Where(t => t.ReferenceType == "TryOn" && t.ReferenceId.HasValue).Select(t => t.ReferenceId!.Value).Distinct().ToList();
            var aiRecomIds = transactions.Where(t => t.ReferenceType == "AIRecommendation" && t.ReferenceId.HasValue).Select(t => t.ReferenceId!.Value).Distinct().ToList();

            Dictionary<int, (string UserName, string Title)> eventMap = new();
            Dictionary<int, string> tryOnMap = new();
            Dictionary<int, string> aiRecomMap = new();

            if (eventIds.Any())
            {
                var events = await _eventRepository.GetByIdsAsync(eventIds);
                eventMap = events.ToDictionary(e => e.EventId, e => (e.Creator?.UserName ?? "N/A", e.Title ?? "N/A"));
            }

            if (tryOnIds.Any())
            {
                var tryOns = await _tryOnHistoryRepository.GetByIdsWithAccountAsync(tryOnIds);
                tryOnMap = tryOns.ToDictionary(h => h.TryOnId, h => h.Account?.UserName ?? "Customer");
            }

            if (aiRecomIds.Any())
            {
                var recoms = await _recommendationHistoryRepository.GetByIdsWithAccountAsync(aiRecomIds);
                aiRecomMap = recoms.ToDictionary(r => r.Id, r => r.Account?.UserName ?? "Customer");
            }

            foreach (var tran in transactions)
            {
                string? displayName = tran.Wallet?.Account?.UserName;
                string? eventName = null;

                if (tran.WalletId == 1 || tran.Type == "Credit")
                {
                    if ((tran.ReferenceType == "Event" || tran.ReferenceType == "EventFix") && tran.ReferenceId.HasValue)
                    {
                        if (eventMap.TryGetValue(tran.ReferenceId.Value, out var eventInfo))
                        {
                            displayName = eventInfo.UserName; 
                            eventName = eventInfo.Title;
                        }
                        else if (tran.EscrowSession?.Sender != null)
                        {
                            displayName = tran.EscrowSession.Sender.UserName;
                            eventName = tran.EscrowSession.Event?.Title;
                        }
                    }
                    else if (tran.ReferenceType == "TryOn" && tran.ReferenceId.HasValue)
                    {
                        if (tryOnMap.TryGetValue(tran.ReferenceId.Value, out var userClient))
                        {
                            displayName = userClient; 
                        }
                        else if (tran.Description != null && tran.Description.Contains("#"))
                        {
                            displayName = $"User #{tran.Description.Split('#').LastOrDefault()}";
                        }
                    }
                    else if (tran.ReferenceType == "AIRecommendation" && tran.ReferenceId.HasValue)
                    {
                        if (aiRecomMap.TryGetValue(tran.ReferenceId.Value, out var userClient))
                        {
                            displayName = userClient; 
                        }
                    }
                    else if (tran.ReferenceType == "OrderPayment" && tran.EscrowSession?.Sender != null)
                    {
                        displayName = tran.EscrowSession.Sender.UserName;
                    }
                }

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
                    UserName = displayName ?? "System / Anonymous",
                    WalletId = tran.WalletId,
                    EventName = eventName
                });
            }

            return responses;
        }
        public async Task<string> AdminBanUser(int accountId)
        {
            var adminId = _currentUserService.GetRequiredUserId();
            if(adminId != 1) return "Only admin can ban users";
            if (adminId == accountId) return "Admin cannot ban themselves";    
            var account = await _accountRepository.GetAccountById(accountId);
            if (account == null) return "Not found";
            if(account.Status == "Banned") return "User is already banned";
            account.Status = "Banned";
            var result = await _accountRepository.UpdateAccount(account);
            if(result != 0)
            {
                return "User banned successfully";
            }
            return "Failed to ban user";
        }
        public async Task<string> AdminUnBanUser(int accountId)
        {
            var adminId = _currentUserService.GetRequiredUserId();
            if (adminId != 1) return "Only admin can unban users";
            if (adminId == accountId) return "Admin cannot unban themselves";
            var account = await _accountRepository.GetAccountById(accountId);
            if (account == null) return "Not found";
            if (account.Status == "Active") return "User is already active";
            account.Status = "Active";
            var result = await _accountRepository.UpdateAccount(account);
            if (result != 0)
            {
                return "User Unbanned successfully";
            }
            return "Failed to Unban user";
        }
    }
}
