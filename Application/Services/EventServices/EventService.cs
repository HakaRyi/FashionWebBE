using Application.Interfaces;
using Application.Jobs;
using Application.Request.EventReq;
using Application.Response.DashboardResp;
using Application.Response.EventResp;
using Application.Response.PostResp;
using Application.Utils.File;
using Domain.Entities;
using Domain.Interfaces;
using Mapster;
using Quartz;

namespace Application.Services.EventServices
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepo;
        private readonly IWalletRepository _walletRepo;
        private readonly IEventExpertRepository _eventExpertRepo;
        private readonly IPostRepository _postRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISchedulerFactory _schedulerFactory;

        public EventService(
            IEventRepository eventRepo,
            IWalletRepository walletRepo,
            IEventExpertRepository eventExpertRepo,
            IPostRepository postRepo,
            IUnitOfWork unitOfWork,
            ISchedulerFactory schedulerFactory,
            ICurrentUserService currentUserService)
        {
            _eventRepo = eventRepo;
            _walletRepo = walletRepo;
            _eventExpertRepo = eventExpertRepo;
            _postRepo = postRepo;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _schedulerFactory = schedulerFactory;
        }

        #region Admin Workflow
        public async Task ApproveEventAsync(int eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev == null || ev.Status != "Pending_Review")
                throw new Exception("Sự kiện không tồn tại hoặc đã được xử lý.");

            await _unitOfWork.BeginTransactionAsync();
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

            await _unitOfWork.BeginTransactionAsync();
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

        public async Task<bool> UpdateEventByAdmin(int eventId, UpdateEventRequestAdmin dto)
        {
            var existingEvent = await _eventRepo.GetByIdAsync(eventId);
            if (existingEvent == null)
            {
                return false;
            }

            existingEvent.Title = dto.Title;
            existingEvent.Description = dto.Description;
            existingEvent.StartTime = dto.StartTime;
            existingEvent.SubmissionDeadline = dto.SubmissionDeadline;
            existingEvent.EndTime = dto.EndTime;

            _eventRepo.Update(existingEvent);

            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0;
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
            int expertId = _currentUserService.GetRequiredUserId();

            // 1. Xác định mốc thời gian khớp với FE truyền lên ("30d", "90d")
            DateTime startDate = period.ToLower() switch
            {
                "30d" => DateTime.Now.AddDays(-30),
                "90d" => DateTime.Now.AddDays(-90),
                _ => DateTime.Now.AddDays(-30) // Mặc định 30 ngày
            };

            // Lấy dữ liệu từ Repo (Nhớ Include Posts và Posts.ExpertRatings)
            var events = await _eventRepo.GetAnalyticsDataAsync(expertId, startDate);
            var eventList = events.ToList();

            var response = new AnalyticsDashboardResponse();

            // 2. Tính toán tổng quan cho StatCards
            int totalPosts = eventList.SelectMany(e => e.Posts).Count();

            // Đếm số lượng bài thi ĐÃ ĐƯỢC CHẤM BỞI CHUYÊN GIA NÀY
            int totalRatedPosts = eventList.SelectMany(e => e.Posts)
                .Count(p => p.ExpertRatings.Any(er => er.ExpertId == expertId));

            // Tổng số lượng Like + Share + Comment từ tất cả các bài thi
            int totalEngagements = eventList.SelectMany(e => e.Posts)
                .Sum(p => (p.LikeCount ?? 0) + (p.ShareCount ?? 0) + (p.CommentCount ?? 0));

            // Tổng chi phí (AppliedFee) chuyên gia đã trả để tạo sự kiện
            decimal totalCost = eventList.Sum(e => e.AppliedFee);

            // Tính % tiến độ chấm bài
            string gradingProgress = totalPosts > 0
                ? $"{(totalRatedPosts * 100.0 / totalPosts):0.0}%"
                : "0%";

            response.Stats = new List<StatCardDto>
    {
        new StatCardDto {
            Label = "Tổng bài dự thi", // Text này phải khớp với iconMap ở FE
            Value = totalPosts.ToString("N0"),
            Change = "+0%", // Có thể làm logic so sánh kỳ trước sau
            IsUp = true
        },
        new StatCardDto {
            Label = "Tiến độ chấm điểm",
            Value = gradingProgress,
            Change = "+0%",
            IsUp = true
        },
        new StatCardDto {
            Label = "Tương tác cộng đồng",
            Value = totalEngagements.ToString("N0"),
            Change = "+0%",
            IsUp = true
        },
        new StatCardDto {
            Label = "Chi phí tạo sự kiện",
            Value = totalCost.ToString("N0") + "đ",
            Change = "-0%", // Chi phí thì đi xuống (giảm) là tốt
            IsUp = false
        }
    };

            // 3. Xử lý TopEvents (Ưu tiên những sự kiện có nhiều bài thi nhất)
            response.TopEvents = eventList
                .OrderByDescending(e => e.Posts.Count)
                .Take(5)
                .Select(e =>
                {
                    int ePosts = e.Posts.Count;
                    int eRated = e.Posts.Count(p => p.ExpertRatings.Any(er => er.ExpertId == expertId));
                    int eEngagements = e.Posts.Sum(p => (p.LikeCount ?? 0) + (p.ShareCount ?? 0) + (p.CommentCount ?? 0));

                    return new TopEventDto
                    {
                        Id = e.EventId,
                        Title = e.Title,
                        Posts = ePosts,
                        Rated = eRated,
                        Engagements = eEngagements >= 1000 ? (eEngagements / 1000.0).ToString("0.1") + "K" : eEngagements.ToString()
                    };
                }).ToList();

            // 4. Xử lý ChartData (Gom nhóm BÀI THI theo ngày để xem biểu đồ tăng trưởng)
            // Lấy tất cả Posts trong sự kiện được tạo sau startDate
            response.ChartData = eventList.SelectMany(e => e.Posts)
                .Where(p => p.CreatedAt.HasValue && p.CreatedAt.Value >= startDate)
                .GroupBy(p => p.CreatedAt.Value.Date) // Group bằng Date gốc để sắp xếp theo thời gian cho chuẩn
                .OrderBy(g => g.Key) // Sắp xếp từ ngày cũ -> ngày mới
                .Select(g => new ChartDataDto
                {
                    Date = g.Key.ToString("dd/MM"), // Format chuỗi để hiển thị trục X
                    Submissions = g.Count(),
                    Engagements = g.Sum(p => (p.LikeCount ?? 0) + (p.ShareCount ?? 0) + (p.CommentCount ?? 0))
                })
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
            bool isCreator = currentUserId == e.CreatorId;

            dto.IsJoined = currentUserId != 0 && e.Posts.Any(p => p.AccountId == currentUserId);
            dto.AcceptedExpertsCount = e.EventExperts?.Count(ex => ex.Status == "Accepted") ?? 0;

            if (isCreator)
            {
                dto.CanManualStart = e.Status == "Inviting" && !e.IsAutoStart && dto.AcceptedExpertsCount >= e.MinExpertsToStart;

                if (dto.CanManualStart && dto.StartTime.HasValue)
                {
                    var startTimeUtc = dto.StartTime.Value.ToUniversalTime();
                    var nowUtc = DateTime.UtcNow;
                    var timeUntilStart = startTimeUtc - nowUtc;

                    if (nowUtc >= startTimeUtc)
                        dto.ReasonManualStart = "Sự kiện đã đến thời gian bắt đầu nhưng chưa kích hoạt.";
                    else if (timeUntilStart.TotalHours > 24)
                    {
                        dto.ReasonManualStart = $"Chỉ có thể kích hoạt thủ công trước giờ bắt đầu tối đa 24 tiếng. (Còn {Math.Round(timeUntilStart.TotalHours, 1)}h nữa mới được phép).";
                        dto.CanManualStart = false;
                    }
                    else
                        dto.ReasonManualStart = "Có thể bắt đầu sự kiện ngay bây giờ.";
                }

                dto.CanFinalize = (e.Status == "Active" || e.Status == "Judging") &&
                                   e.EndTime.HasValue &&
                                   DateTime.UtcNow >= e.EndTime.Value.ToUniversalTime();

                dto.IsCreator = isCreator;
            }
            else
            {
                dto.AppliedFee = 0;
                dto.Experts = dto.Experts.Where(ex => ex.Status == "Accepted").ToList();
                dto.CanManualStart = false;
                dto.CanFinalize = false;
                dto.IsCreator = false;
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
                SubmissionDeadline = e.SubmissionDeadline,
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


                ParticipantCount = e.Posts?.Count ?? 0,

                ThumbnailUrl = e.Images?.OrderBy(i => i.ImageId).FirstOrDefault()?.ImageUrl,

                TotalPrizePool = e.PrizeEvents?.Sum(p => p.RewardAmount) ?? 0,
                Prizes = e.PrizeEvents?
                    .OrderBy(p => p.Ranked)
                    .Select(p => new PrizeBriefDto
                    {
                        Ranked = p.Ranked,
                        RewardAmount = p.RewardAmount
                    }).ToList() ?? new List<PrizeBriefDto>(),

                IsJoined = currentUserId.HasValue && e.Posts.Any(p => p.AccountId == currentUserId.Value),

                MyExpertStatus = currentUserId.HasValue
                    ? e.EventExperts?.FirstOrDefault(ee => ee.ExpertId == currentUserId.Value)?.Status
                    : null
            };
        }

        public async Task<List<EventLeaderboardDto>> GetEventLeaderboardAsync(int eventId)
        {
            var eventDetail = await _eventRepo.GetByIdAsync(eventId);
            var scores = await _eventRepo.GetLeaderboardAsync(eventId);
            return scores.Select((s, index) =>
            {
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
            var userId = _currentUserService.GetUserId() ?? 0;
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
        #endregion
    }
}