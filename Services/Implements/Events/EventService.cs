using Mapster;
using Quartz;
using Repositories.Entities;
using Repositories.Repos.EscrowSessionRepos;
using Repositories.Repos.EventExpertRepos;
using Repositories.Repos.Events;
using Repositories.Repos.EventWinnerRepos;
using Repositories.Repos.ExpertRatingRepos;
using Repositories.Repos.ImageRepos;
using Repositories.Repos.PostRepos;
using Repositories.Repos.PrizeEventRepos;
using Repositories.Repos.ScoreboardRepos;
using Repositories.Repos.SystemSettingRepos;
using Repositories.Repos.TransactionRepos;
using Repositories.Repos.WalletRepos;
using Repositories.UnitOfWork;
using Services.Implements.Auth;
using Services.Jobs;
using Services.Request.EventReq;
using Services.Request.ExpertRatingReq;
using Services.Request.PrizeReq;
using Services.Response.DashboardResp;
using Services.Response.EventResp;
using Services.Response.PostResp;
using Services.Utils.File;
using System.Linq;

namespace Services.Implements.Events
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepo;
        private readonly IWalletRepository _walletRepo;
        private readonly IPrizeEventRepository _prizeRepo;
        private readonly ITransactionRepository _transactionRepo;
        private readonly IEscrowSessionRepository _escrowRepo;
        private readonly IEventExpertRepository _eventExpertRepo;
        private readonly IExpertRatingRepository _ratingRepo;
        private readonly IScoreboardRepository _scoreboardRepo;
        private readonly IPostRepository _postRepo;
        private readonly IEventWinnerRepository _winnerRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IImageRepository _imageRepo;
        private readonly IFileService _fileService;
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly ISystemSettingRepository _settingRepo;

        public EventService(
            IEventRepository eventRepo,
            IWalletRepository walletRepo,
            IPrizeEventRepository prizeRepo,
            ITransactionRepository transactionRepo,
            IEscrowSessionRepository escrowRepo,
            IEventExpertRepository eventExpertRepo,
            IExpertRatingRepository ratingRepo,
            IScoreboardRepository scoreboardRepo,
            IPostRepository postRepo,
            IEventWinnerRepository winnerRepo,
            IUnitOfWork unitOfWork,
            IImageRepository imageRepo,
            ISystemSettingRepository settingRepo,
            IFileService fileService,
            ISchedulerFactory schedulerFactory,
            ICurrentUserService currentUserService)
        {
            _eventRepo = eventRepo;
            _walletRepo = walletRepo;
            _prizeRepo = prizeRepo;
            _transactionRepo = transactionRepo;
            _escrowRepo = escrowRepo;
            _eventExpertRepo = eventExpertRepo;
            _ratingRepo = ratingRepo;
            _scoreboardRepo = scoreboardRepo;
            _postRepo = postRepo;
            _winnerRepo = winnerRepo;
            _settingRepo = settingRepo;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _imageRepo = imageRepo;
            _fileService = fileService;
            _schedulerFactory = schedulerFactory;
        }

        #region Admin Workflow
        public async Task ApproveEventAsync(int eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev == null || ev.Status != "Pending_Review")
                throw new Exception("Sự kiện không tồn tại hoặc đã được xử lý.");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                ev.Status = "Inviting";
                _eventRepo.Update(ev);

                var experts = await _eventExpertRepo.GetByEventIdAsync(eventId);
                foreach (var exp in experts.Where(e => e.Status == "Awaiting_Review"))
                {
                    exp.Status = "Pending";
                    _eventExpertRepo.Update(exp);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();

                if (ev.IsAutoStart)
                {
                    await ScheduleEventActivation(ev);
                }
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Admin từ chối sự kiện: Hoàn tiền từ LockedBalance về Balance và cập nhật trạng thái
        /// </summary>
        public async Task RejectEventAsync(int eventId, string reason)
        {
            // 1. Lấy thông tin sự kiện và kiểm tra điều kiện
            var ev = await _eventRepo.GetByIdAsync(eventId);

            if (ev == null)
                throw new KeyNotFoundException("Không tìm thấy sự kiện.");

            if (ev.Status != "Pending_Review")
                throw new InvalidOperationException("Chỉ có thể từ chối sự kiện đang ở trạng thái 'Chờ duyệt'.");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Vui lòng cung cấp lý do từ chối.");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 3. Tính toán số tiền cần hoàn lại
                // Refund = Phí áp dụng + Tổng các giải thưởng đã nạp
                decimal totalPrizePool = ev.PrizeEvents?.Sum(p => p.RewardAmount) ?? 0;
                decimal totalToRefund = totalPrizePool + ev.AppliedFee;

                var wallet = await _walletRepo.GetByAccountIdAsync(ev.CreatorId);
                if (wallet == null)
                    throw new Exception("Không tìm thấy ví của người tạo sự kiện.");

                if (wallet.LockedBalance < totalToRefund)
                    throw new Exception("Số dư bị khóa không đủ để thực hiện hoàn tiền (Lỗi logic dữ liệu).");

                // Chuyển tiền từ 'Bị khóa' về lại 'Số dư khả dụng'
                wallet.LockedBalance -= totalToRefund;
                wallet.Balance += totalToRefund;

                ev.Status = "Rejected";
                ev.Note = reason;

                _eventRepo.Update(ev);
                _walletRepo.Update(wallet);

                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitAsync();

                // TODO: Gửi Notification cho Creator ở đây (nếu có hệ thống thông báo)
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception($"Lỗi khi từ chối sự kiện: {ex.Message}");
            }
        }

        #endregion

        #region Helpers
        private async Task ScheduleEventActivation(Event ev)
        {
            // 1. Lấy bộ lập lịch từ Factory (đã inject ở Constructor)
            var scheduler = await _schedulerFactory.GetScheduler();

            // 2. Định nghĩa Job và truyền dữ liệu (EventId) vào JobDataMap
            var job = JobBuilder.Create<ActivateEventJob>()
                .WithIdentity($"Job_Activate_{ev.EventId}", "EventGroup")
                .WithDescription($"Kích hoạt sự kiện: {ev.Title} (ID: {ev.EventId})")
                .UsingJobData("EventId", ev.EventId)
                .Build();

            // 3. Tạo Trigger để xác định thời điểm chạy
            // Sử dụng StartTime của Event, nếu không có thì mặc định chạy sau 30 giây
            var startTimeRaw = ev.StartTime ?? DateTime.Now.AddSeconds(30);
            DateTime processTime = startTimeRaw.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(startTimeRaw, DateTimeKind.Local)
                : startTimeRaw;

            DateTimeOffset startTimeOffset = new DateTimeOffset(processTime);

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"Trigger_Activate_{ev.EventId}", "EventGroup")
                .WithDescription($"Lịch kích hoạt cho sự kiện '{ev.Title}'")
                .StartAt(startTimeOffset) // Truyền Offset vào đây
                .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                .Build();

            await scheduler.ScheduleJob(job, trigger);
        }
        #endregion

        #region Get Methods
        public async Task<IEnumerable<PostReviewDto>> GetPostsForReviewAsync(int eventId)
        {
            int currentExpertId = _currentUserService.GetRequiredUserId();

            // Đảm bảo PostRepo.GetPostsByEventIdAsync đã .Include(p => p.ExpertRatings)
            var posts = await _postRepo.GetPostsByEventIdAsync(eventId);

            return posts.Select(p =>
            {
                // Tìm bản ghi chấm điểm của Expert hiện tại
                var myRating = p.ExpertRatings?.FirstOrDefault(r => r.ExpertId == currentExpertId);

                return new PostReviewDto
                {
                    PostId = p.PostId,
                    Title = p.Title ?? "Untitled",
                    Content = p.Content,
                    ImageUrl = p.Images.OrderBy(i => i.ImageId).FirstOrDefault()?.ImageUrl,
                    AuthorName = p.Account?.UserName,

                    // Thông tin chấm điểm
                    CurrentScore = myRating?.Score,
                    MyReason = myRating?.Reason,
                    IsGraded = myRating != null,

                    // Thông số khác
                    LikeCount = p.LikeCount ?? 0,
                    ShareCount = p.ShareCount ?? 0,
                    SubmittedAt = p.CreatedAt ?? DateTime.Now
                };
            }).ToList();
        }

        public async Task<AnalyticsDashboardResponse> GetAnalyticsAsync(string period)
        {
            int creatorId = _currentUserService.GetRequiredUserId();

            // 1. Xác định mốc thời gian (Period)
            DateTime startDate = period.ToLower() switch
            {
                "7days" => DateTime.Now.AddDays(-7),
                "30days" => DateTime.Now.AddDays(-30),
                "90days" => DateTime.Now.AddDays(-90),
                _ => DateTime.Now.AddDays(-30)
            };

            // Lấy dữ liệu từ Repo (Đảm bảo Repo đã Include Posts và PrizeEvents)
            var events = await _eventRepo.GetAnalyticsDataAsync(creatorId, startDate);
            var eventList = events.ToList();

            var response = new AnalyticsDashboardResponse();

            // 2. Xử lý StatCards (Tổng hợp số liệu)
            int totalEvents = eventList.Count;
            int totalParticipants = eventList.Sum(e => e.Posts.Count);
            decimal totalPrizes = eventList.Sum(e => e.PrizeEvents.Sum(p => p.RewardAmount));

            response.Stats = new List<StatCardDto>
    {
        new StatCardDto {
            Label = "Tổng sự kiện",
            Value = totalEvents.ToString(),
            Change = "+12%", // Phần trăm này thường so sánh với kỳ trước (logic nâng cao)
            IsUp = true
        },
        new StatCardDto {
            Label = "Người tham gia",
            Value = totalParticipants.ToString(),
            Change = "+5.4%",
            IsUp = true
        },
        new StatCardDto {
            Label = "Ngân sách giải thưởng",
            Value = totalPrizes.ToString("N0") + " VNĐ",
            Change = "-2.1%",
            IsUp = false
        }
    };

            // 3. Xử lý TopEvents (Sắp xếp theo số lượng bài tham gia nhiều nhất)
            response.TopEvents = eventList
                .OrderByDescending(e => e.Posts.Count)
                .Take(5)
                .Select(e => new TopEventDto
                {
                    Id = e.EventId,
                    Title = e.Title,
                    Views = (e.Posts.Count * 1.5).ToString("N0"), // Giả định view dựa trên post
                    Progress = e.Status == "Completed" ? 100 : 75 // Giả định tiến độ dựa trên trạng thái
                }).ToList();

            // 4. Xử lý ChartData (Gom nhóm theo ngày để vẽ biểu đồ tăng trưởng)
            response.ChartData = eventList
                .GroupBy(e => e.CreatedAt?.ToString("dd/MM"))
                .Select(g => new ChartDataDto
                {
                    Name = g.Key ?? "N/A",
                    Value = g.Count() // Số lượng sự kiện tạo mới mỗi ngày
                })
                .OrderBy(g => g.Name)
                .ToList();

            return response;
        }

        /// <summary>
        /// Expert xem danh sách các sự kiện do chính họ tạo ra (Tất cả trạng thái)
        /// </summary>
        public async Task<IEnumerable<EventListDto>> GetMyCreatedEventsAsync()
        {
            int expertId = _currentUserService.GetRequiredUserId();

            var events = await _eventRepo.GetAllByCreatorIdAsync(expertId);

            return events.Adapt<IEnumerable<EventListDto>>();
        }

        /// <summary>
        /// Lấy chi tiết một Event kèm theo các Prize và Expert liên quan
        /// </summary>
        public async Task<EventDetailDto?> GetEventDetailsAsync(int eventId)
        {
            int currentUserId = _currentUserService.GetUserId() ?? 0;

            var e = await _eventRepo.GetByIdAsync(eventId);
            if (e == null) return null;

            var dto = e.Adapt<EventDetailDto>();

            bool isCreator = (currentUserId == e.CreatorId);

            dto.IsJoined = currentUserId != 0 && e.Posts.Any(p => p.AccountId == currentUserId);
            dto.AcceptedExpertsCount = e.EventExperts?.Count(ex => ex.Status == "Accepted") ?? 0;

            dto.CanManualStart = isCreator &&
                                 e.Status == "Inviting" &&
                                 !e.IsAutoStart &&
                                 dto.AcceptedExpertsCount >= e.MinExpertsToStart;

            dto.CanFinalize = isCreator &&
                  (e.Status == "Active" || e.Status == "Judging") &&
                  e.EndTime.HasValue &&
                  DateTime.UtcNow >= e.EndTime.Value.ToUniversalTime();

            if (!isCreator)
            {
                dto.AppliedFee = 0;

                dto.Experts = dto.Experts.Where(ex => ex.Status == "Accepted").ToList();
                dto.CanManualStart = false;
                dto.CanFinalize = false;
            }

            return dto;
        }

        /// <summary>
        /// DÀNH CHO USER: Chỉ thấy sự kiện đang mời, đang chạy hoặc đã xong
        /// </summary>
        public async Task<IEnumerable<EventListDto>> GetAllEventsForUserAsync()
        {
            // Lọc ngay tại SQL thông qua statuses parameter
            var publicStatuses = new[] { "Inviting", "Active", "Completed" };
            var events = await _eventRepo.GetAllAsync(publicStatuses);

            return events.Select(MapToEventListDto);
        }

        /// <summary>
        /// DÀNH CHO EXPERT: Thấy tổng hợp (Tạo HOẶC Mời)
        /// </summary>
        public async Task<IEnumerable<EventListDto>> GetAllEventsForExpertAsync()
        {
            int currentExpertId = _currentUserService.GetRequiredUserId();
            var events = await _eventRepo.GetExpertRelatedEventsAsync(currentExpertId);

            return events.Select(MapToEventListDto);
        }

        /// <summary>
        /// DÀNH CHO ADMIN: Thấy TẤT CẢ trạng thái
        /// </summary>
        public async Task<IEnumerable<EventAdminListDto>> GetAllEventsForAdminAsync()
        {
            // statuses = null để lấy tất cả
            var events = await _eventRepo.GetAllAsync();

            return events.Select(e => new EventAdminListDto
            {
                EventId = e.EventId,
                Title = e.Title,
                Status = e.Status,
                Note = e.Note,
                CreatorId = e.CreatorId,
                CreatorName = e.Creator?.UserName,
                CreatorEmail = e.Creator?.Email,
                AppliedFee = e.AppliedFee,
                TotalPrizePool = e.PrizeEvents?.Sum(p => p.RewardAmount) ?? 0,
                MinExperts = e.MinExpertsToStart,
                CurrentAcceptedExperts = e.EventExperts?.Count(ee => ee.Status == "Accepted") ?? 0,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                CreatedAt = e.CreatedAt,
                ParticipantCount = e.Posts?.Count ?? 0,
                ThumbnailUrl = e.Images?.FirstOrDefault()?.ImageUrl
            });
        }

        private EventListDto MapToEventListDto(Event e)
        {
            // Lấy UserId của người đang đăng nhập (nếu có)
            int? currentUserId = _currentUserService.GetUserId();

            return new EventListDto
            {
                EventId = e.EventId,
                Title = e.Title,
                Description = e.Description,
                Status = e.Status,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                CreatedAt = e.CreatedAt,
                CreatorName = e.Creator?.UserName,
                CreatorAvatarUrl = e.Creator?.Avatars.OrderByDescending(img => img.CreatedAt)
                    .Select(img => img.ImageUrl)
                    .FirstOrDefault() ?? null,


                // 1. Đếm số lượng bài tham gia
                ParticipantCount = e.Posts?.Count ?? 0,

                // 2. Lấy ảnh đầu tiên làm thumbnail
                ThumbnailUrl = e.Images?.OrderBy(i => i.ImageId).FirstOrDefault()?.ImageUrl,

                // 3. Tính toán giải thưởng rõ ràng
                TotalPrizePool = e.PrizeEvents?.Sum(p => p.RewardAmount) ?? 0,
                Prizes = e.PrizeEvents?
                    .OrderBy(p => p.Ranked)
                    .Select(p => new PrizeBriefDto
                    {
                        Ranked = p.Ranked,
                        RewardAmount = p.RewardAmount
                    }).ToList() ?? new List<PrizeBriefDto>(),

                // 4. Kiểm tra trạng thái cá nhân hóa
                IsJoined = currentUserId.HasValue && e.Posts.Any(p => p.AccountId == currentUserId.Value),

                MyExpertStatus = currentUserId.HasValue
                    ? e.EventExperts?.FirstOrDefault(ee => ee.ExpertId == currentUserId.Value)?.Status
                    : null
            };
        }

        #endregion

        public async Task<List<EventLeaderboardDto>> GetEventLeaderboardAsync(int eventId)
        {
            var eventDetail = await _eventRepo.GetByIdAsync(eventId);
            var scores = await _eventRepo.GetLeaderboardAsync(eventId);
            return scores.Select((s, index) => {
                int rank = index + 1;
                var prize = eventDetail?.PrizeEvents?.FirstOrDefault(p => p.Ranked == rank);

                return new EventLeaderboardDto
                {
                    Rank = rank,
                    AccountId = s.Post.AccountId,
                    UserName = s.Post.Account.UserName ?? "Anonymous",
                    AvatarUrl = s.Post.Account.Avatars.OrderByDescending(a => a.CreatedAt).FirstOrDefault()?.ImageUrl,
                    FinalScore = s.FinalScore,
                    PostId = s.PostId,
                    RewardAmount = prize?.RewardAmount
                };
            }).ToList();
        }

        public async Task<MyEventResultDetailDto?> GetMyResultDetailAsync(int eventId)
        {
            var userId = _currentUserService.GetUserId()??0;
            var myScore = await _eventRepo.GetUserScoreAsync(eventId, userId);
            if (myScore == null) return null;

            var leaderboard = await GetEventLeaderboardAsync(eventId);
            int myRank = leaderboard.FirstOrDefault(x => x.AccountId == userId)?.Rank ?? 0;

            var ratings = await _eventRepo.GetExpertRatingsForPostAsync(myScore.PostId);
            var reactions = await _eventRepo.GetPostVotersAsync(myScore.PostId);

            // Lấy ảnh bài post của chính mình
            var post = await _postRepo.GetByIdAsync(myScore.PostId);

            return new MyEventResultDetailDto
            {
                Rank = myRank,
                MyScore = myScore.FinalScore,
                MyPostImageUrl = post?.Images.FirstOrDefault()?.ImageUrl,
                ExpertReviews = ratings.Select(r => new ExpertReviewDto
                {
                    ExpertName = r.Expert.UserName ?? "Expert",
                    ExpertAvatar = r.Expert.Avatars.OrderByDescending(a => a.CreatedAt).FirstOrDefault()?.ImageUrl,
                    Score = r.Score,
                    Reason = r.Reason
                }).ToList(),
                Voters = reactions.Select(re => new VoterDto
                {
                    UserName = re.Account.UserName ?? "Voter",
                    AvatarUrl = re.Account.Avatars.OrderByDescending(a => a.CreatedAt).FirstOrDefault()?.ImageUrl,
                    VotedAt = re.CreatedAt ?? DateTime.Now
                }).ToList()
            };
        }
    } 
}