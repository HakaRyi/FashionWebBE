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
using Services.Utils.File;

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


        #region Quản lý Sự kiện và Ký quỹ
        public async Task<Event> CreateEventAsync(CreateEventRequest dto)
        {
            int creatorId = _currentUserService.GetRequiredUserId();
            ValidateEventRequest(dto);

            decimal currentFee = await _settingRepo.GetDecimalValueAsync("EventCreationFee", 10000);
            var totalPrize = dto.Prizes.Sum(p => p.RewardAmount);

            var wallet = await _walletRepo.GetByAccountIdAsync(creatorId);
            if (wallet == null || wallet.Balance < (totalPrize + currentFee))
                throw new Exception($"Số dư ví không đủ. Cần {(totalPrize + currentFee):N0} VNĐ (bao gồm phí tạo).");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                string? imageUrl = dto.ImageFile != null ? await _fileService.UploadAsync(dto.ImageFile) : null;

                var eventData = MapToEvent(dto, creatorId, imageUrl);
                eventData.AppliedFee = currentFee;

                await _eventRepo.AddAsync(eventData);
                await _unitOfWork.SaveChangesAsync();

                await CreatePrizesAsync(eventData.EventId, dto.Prizes);
                await SetupExpertPanelAsync(eventData.EventId, creatorId, dto.InvitedExpertIds);

                decimal totalToLock = totalPrize + currentFee;
                wallet.Balance -= totalToLock;
                wallet.LockedBalance += totalToLock;
                _walletRepo.Update(wallet);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();

                await ScheduleEventActivation(eventData);
                return eventData;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception($"Lỗi tạo Event: {ex.Message}");
            }
        }

        public async Task ActivateEventWithEscrowAsync(int eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev == null || ev.Status != "Pending_Payment") return;

            var experts = await _eventExpertRepo.GetByEventIdAsync(eventId);
            int acceptedCount = experts.Count(e => e.Status == "Accepted");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var wallet = await _walletRepo.GetByAccountIdAsync(ev.CreatorId);

                // TRƯỜNG HỢP 1: Không đủ Expert -> Hủy và Hoàn trả toàn bộ tiền đã khóa
                if (acceptedCount < ev.MinExpertsToStart)
                {
                    var prizes = await _prizeRepo.GetByEventIdAsync(eventId);
                    decimal totalToRefund = prizes.Sum(p => p.RewardAmount) + ev.AppliedFee;

                    wallet.LockedBalance -= totalToRefund;
                    wallet.Balance += totalToRefund;

                    ev.Status = "Cancelled_NotEnoughExperts";
                    _eventRepo.Update(ev);
                    _walletRepo.Update(wallet);

                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitAsync();
                    return;
                }

                // TRƯỜNG HỢP 2: Đủ điều kiện -> Kích hoạt
                var prizesData = await _prizeRepo.GetByEventIdAsync(eventId);
                decimal totalPrizeAmount = prizesData.Sum(p => p.RewardAmount);

                // A. Thu phí hệ thống (Từ Locked sang ví Admin)
                await CollectSystemFeeAsync(wallet, ev);

                // B. Chuyển tiền giải thưởng vào Escrow (Từ Locked sang Escrow)
                await ProcessEscrowFromLockedAsync(eventId, ev.CreatorId, totalPrizeAmount, wallet);

                ev.Status = "Active";
                _eventRepo.Update(ev);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync();
            }
        }

        private async Task CollectSystemFeeAsync(Wallet expertWallet, Event ev)
        {
            int adminAccountId = await _settingRepo.GetIntValueAsync("SystemAdminAccountId", 1);
            var adminWallet = await _walletRepo.GetByAccountIdAsync(adminAccountId);

            if (adminWallet == null) throw new Exception("Không tìm thấy ví hệ thống.");

            // Trừ từ tiền đã khóa của Expert
            expertWallet.LockedBalance -= ev.AppliedFee;

            decimal adminBefore = adminWallet.Balance;
            adminWallet.Balance += ev.AppliedFee;

            _walletRepo.Update(expertWallet);
            _walletRepo.Update(adminWallet);

            // Log giao dịch doanh thu cho Admin
            await _transactionRepo.AddAsync(new Transaction
            {
                WalletId = adminWallet.WalletId,
                Amount = ev.AppliedFee,
                BalanceBefore = adminBefore,
                BalanceAfter = adminWallet.Balance,
                Type = "System_Fee_Revenue",
                ReferenceId = ev.EventId,
                ReferenceType = "Event",
                Status = "Success",
                CreatedAt = DateTime.Now
            });
        }

        private async Task ProcessEscrowFromLockedAsync(int eventId, int expertId, decimal amount, Wallet wallet)
        {
            // Trừ từ tiền đã khóa
            wallet.LockedBalance -= amount;
            _walletRepo.Update(wallet);

            await _escrowRepo.AddAsync(new EscrowSession
            {
                EventId = eventId,
                SenderId = expertId,
                Amount = amount,
                Status = "Held",
                CreatedAt = DateTime.Now
            });

            await _transactionRepo.AddAsync(new Transaction
            {
                WalletId = wallet.WalletId,
                Amount = -amount,
                Type = "Escrow_Hold",
                ReferenceId = eventId,
                ReferenceType = "Event",
                Status = "Success",
                CreatedAt = DateTime.Now
            });
        }
        #endregion

        #region Helpers
        private Event MapToEvent(CreateEventRequest dto, int creatorId, string? imageUrl)
        {
            var ev = new Event
            {
                Title = dto.Title,
                Description = dto.Description,
                CreatorId = creatorId,
                ExpertWeight = dto.ExpertWeight,
                UserWeight = dto.UserWeight,
                PointPerLike = dto.PointPerLike,
                PointPerShare = dto.PointPerShare,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                CreatedAt = DateTime.Now,
                MinExpertsToStart = dto.MinExpertsRequired,
                Status = "Pending_Payment"
            };

            if (!string.IsNullOrEmpty(imageUrl))
            {
                ev.Images.Add(new Image
                {
                    ImageUrl = imageUrl,
                    OwnerType = "Event_Thumbnail",
                    CreatedAt = DateTime.Now
                });
            }
            return ev;
        }

        private async Task ScheduleEventActivation(Event ev)
        {
            // 1. Lấy bộ lập lịch từ Factory (đã inject ở Constructor)
            var scheduler = await _schedulerFactory.GetScheduler();

            // 2. Định nghĩa Job và truyền dữ liệu (EventId) vào JobDataMap
            var job = JobBuilder.Create<ActivateEventJob>()
                .WithIdentity($"Job_Activate_{ev.EventId}", "EventGroup")
                .UsingJobData("EventId", ev.EventId)
                .Build();

            // 3. Tạo Trigger để xác định thời điểm chạy
            // Sử dụng StartTime của Event, nếu không có thì mặc định chạy sau 30 giây
            var startTime = ev.StartTime ?? DateTime.Now.AddSeconds(30);

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"Trigger_Activate_{ev.EventId}", "EventGroup")
                .StartAt(startTime)
                .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                .Build();

            await scheduler.ScheduleJob(job, trigger);
        }

        private void ValidateEventRequest(CreateEventRequest dto)
        {
            if (Math.Abs(dto.ExpertWeight + dto.UserWeight - 1.0) > 0.001)
                throw new Exception("Tổng trọng số Expert và User phải bằng 1.0.");

            if (dto.MinExpertsRequired < 2)
                throw new Exception("Số lượng Expert yêu cầu tối thiểu không được dưới 2.");
        }

        private async Task CreatePrizesAsync(int eventId, List<PrizeRequest> prizeRequests)
        {
            var prizes = prizeRequests.Select(p => new PrizeEvent
            {
                EventId = eventId,
                Ranked = p.Ranked,
                RewardAmount = p.RewardAmount,
                Status = "Active"
            }).ToList();
            await _prizeRepo.AddRangeAsync(prizes);
        }

        private async Task SetupExpertPanelAsync(int eventId, int creatorId, List<int>? invitedIds)
        {
            var expertPanel = new List<EventExpert> {
                new EventExpert { EventId = eventId, ExpertId = creatorId, JoinedAt = DateTime.Now, Status = "Accepted" }
            };

            if (invitedIds != null)
            {
                expertPanel.AddRange(invitedIds.Distinct().Where(id => id != creatorId).Select(id => new EventExpert
                {
                    EventId = eventId,
                    ExpertId = id,
                    JoinedAt = DateTime.Now,
                    Status = "Pending"
                }));
            }
            await _eventExpertRepo.AddRangeAsync(expertPanel);
        }
        #endregion

        #region Logic Chấm điểm Hội đồng
        public async Task SubmitExpertRatingAsync(ExpertRatingRequest dto)
        {
            int currentExpertId = _currentUserService.GetRequiredUserId();

            var post = await _postRepo.GetByIdAsync(dto.PostId);
            if (post == null || post.EventId == null) throw new Exception("Bài viết không thuộc sự kiện nào.");

            var ev = await _eventRepo.GetByIdAsync(post.EventId.Value);
            if (ev == null || ev.Status != "Active") throw new Exception("Sự kiện đã kết thúc hoặc không tồn tại.");

            var isMember = await _eventExpertRepo.AnyAsync(ee =>
                ee.EventId == post.EventId && ee.ExpertId == currentExpertId && ee.Status == "Accepted");

            if (!isMember) throw new Exception("Bạn không có quyền chấm điểm cho sự kiện này.");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var existingRating = await _ratingRepo.GetByPostAndExpertAsync(dto.PostId, currentExpertId);
                if (existingRating != null)
                {
                    existingRating.Score = dto.Score;
                    existingRating.Reason = dto.Reason;
                    existingRating.UpdatedAt = DateTime.Now;
                    _ratingRepo.Update(existingRating);
                }
                else
                {
                    await _ratingRepo.AddAsync(new ExpertRating
                    {
                        PostId = dto.PostId,
                        ExpertId = currentExpertId,
                        Score = dto.Score,
                        Reason = dto.Reason,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });
                }
                await _unitOfWork.SaveChangesAsync();

                await RecalculateEventScoreboardAsync(post.EventId.Value);

                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception($"Lỗi chấm điểm: {ex.Message}");
            }
        }

        private async Task RecalculateEventScoreboardAsync(int eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);

            var allPosts = (await _postRepo.GetPostsByEventIdAsync(eventId)).ToList();
            if (!allPosts.Any()) return;

            // 1. Tính Raw Community Score cho từng bài và tìm Max
            var rawScores = allPosts.Select(p => new {
                PostId = p.PostId,
                RawValue = (p.LikeCount ?? 0) * ev.PointPerLike + (p.ShareCount ?? 0) * ev.PointPerShare
            }).ToList();

            double maxRawScore = rawScores.Max(s => s.RawValue);
            if (maxRawScore == 0) maxRawScore = 1;

            // 2. update each Scoreboard
            foreach (var p in allPosts)
            {
                var ratings = await _ratingRepo.GetRatingsByPostIdAsync(p.PostId);

                double avgExpertScore = ratings.Any() ? ratings.Average(r => r.Score) : 0;

                double currentRaw = (p.LikeCount ?? 0) * ev.PointPerLike + (p.ShareCount ?? 0) * ev.PointPerShare;
                double normalizedCommunityScore = (currentRaw / maxRawScore) * 10;

                double finalScore = (avgExpertScore * ev.ExpertWeight) + (normalizedCommunityScore * ev.UserWeight);

                var sb = await _scoreboardRepo.GetByPostIdAsync(p.PostId);
                if (sb == null)
                {
                    await _scoreboardRepo.AddAsync(new Scoreboard
                    {
                        PostId = p.PostId,
                        ExpertScore = avgExpertScore,
                        CommunityScore = normalizedCommunityScore,
                        FinalScore = finalScore,
                        FinalLikeCount = p.LikeCount ?? 0,
                        FinalShareCount = p.ShareCount ?? 0,
                        CreatedAt = DateTime.Now,
                        Status = "Judging"
                    });
                }
                else
                {
                    sb.ExpertScore = avgExpertScore;
                    sb.CommunityScore = normalizedCommunityScore;
                    sb.FinalScore = finalScore;
                    sb.FinalLikeCount = p.LikeCount ?? 0;
                    sb.FinalShareCount = p.ShareCount ?? 0;
                    sb.Status = "Judging";
                    _scoreboardRepo.Update(sb);
                }
            }
            await _unitOfWork.SaveChangesAsync();
        }
        #endregion

        #region Logic Giải ngân
        public async Task FinalizeEventAndDistributePrizesAsync(int eventId)
        {
            int creatorId = _currentUserService.GetRequiredUserId();

            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev == null) throw new Exception("Sự kiện không tồn tại.");
            if (ev.CreatorId != creatorId) throw new Exception("Chỉ người tạo sự kiện mới có quyền chốt giải.");
            if (ev.Status != "Active") throw new Exception("Sự kiện đã kết thúc hoặc không ở trạng thái hoạt động.");

            var prizes = (await _prizeRepo.GetByEventIdAsync(eventId)).OrderBy(p => p.Ranked).ToList();
            var leaderboard = (await _scoreboardRepo.GetLeaderboardByEventIdAsync(eventId)).ToList();

            if (!leaderboard.Any()) throw new Exception("Không có bài dự thi nào để trao giải.");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var escrow = await _escrowRepo.GetByEventIdAsync(eventId);
                if (escrow == null || escrow.Status != "Held") throw new Exception("Không tìm thấy khoản ký quỹ hợp lệ.");

                for (int i = 0; i < prizes.Count; i++)
                {
                    if (i >= leaderboard.Count) break;

                    var prize = prizes[i];
                    var winnerScore = leaderboard[i];
                    var winnerPost = await _postRepo.GetByIdAsync(winnerScore.PostId);
                    var winnerWallet = await _walletRepo.GetByAccountIdAsync(winnerPost.AccountId);

                    if (winnerWallet == null) continue;

                    await _winnerRepo.AddAsync(new EventWinner
                    {
                        AccountId = winnerPost.AccountId,
                        PrizeEventId = prize.PrizeEventId,
                        WinningScore = winnerScore.FinalScore,
                        FinalRank = prize.Ranked,
                        ExpertFeedback = winnerScore.ExpertReason,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        Status = "Paid"
                    });

                    decimal balanceBefore = winnerWallet.Balance;
                    winnerWallet.Balance += prize.RewardAmount;
                    _walletRepo.Update(winnerWallet);

                    await _transactionRepo.AddAsync(new Transaction
                    {
                        WalletId = winnerWallet.WalletId,
                        Amount = prize.RewardAmount,
                        BalanceBefore = balanceBefore,
                        BalanceAfter = winnerWallet.Balance,
                        Type = "Event_Prize_Payout",
                        ReferenceType = "PrizeEvent",
                        ReferenceId = prize.PrizeEventId,
                        Status = "Success",
                        CreatedAt = DateTime.Now
                    });

                    prize.Status = "Distributed";
                    _prizeRepo.Update(prize);
                }

                ev.Status = "Completed";
                _eventRepo.Update(ev);

                escrow.Status = "Resolved";
                escrow.ResolvedAt = DateTime.Now;
                _escrowRepo.Update(escrow);

                decimal paidAmount = prizes.Where(p => p.Status == "Distributed").Sum(p => p.RewardAmount);
                decimal refundAmount = escrow.Amount - paidAmount;

                if (refundAmount > 0)
                {
                    var creatorWallet = await _walletRepo.GetByAccountIdAsync(creatorId);
                    if (creatorWallet != null)
                    {
                        decimal creatorBalanceBefore = creatorWallet.Balance;
                        creatorWallet.Balance += refundAmount;
                        _walletRepo.Update(creatorWallet);

                        await _transactionRepo.AddAsync(new Transaction
                        {
                            WalletId = creatorWallet.WalletId,
                            Amount = refundAmount,
                            BalanceBefore = creatorBalanceBefore,
                            BalanceAfter = creatorWallet.Balance,
                            Type = "Event_Refund",
                            ReferenceType = "Event",
                            ReferenceId = ev.EventId,
                            Status = "Success",
                            CreatedAt = DateTime.Now
                        });
                    }
                }

                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception($"Lỗi khi giải ngân giải thưởng: {ex.Message}");
            }
        }
        #endregion

        #region Logic Truy vấn (Get Methods)

        /// <summary>
        /// Expert xem danh sách các sự kiện do chính họ tạo ra (Tất cả trạng thái)
        /// </summary>
        public async Task<IEnumerable<Event>> GetMyCreatedEventsAsync()
        {
            int expertId = _currentUserService.GetRequiredUserId();
            return await _eventRepo.GetAllByCreatorIdAsync(expertId);
        }

        /// <summary>
        /// Expert xem danh sách các sự kiện mà họ được mời tham gia Hội đồng chấm điểm
        /// </summary>
        public async Task<IEnumerable<Event>> GetEventsInvitedToRateAsync()
        {
            int currentExpertId = _currentUserService.GetRequiredUserId();

            var eventIds = await _eventExpertRepo.GetEventIdsByExpertIdAsync(currentExpertId);

            var events = new List<Event>();
            foreach (var id in eventIds)
            {
                var ev = await _eventRepo.GetByIdAsync(id);
                if (ev != null) events.Add(ev);
            }
            return events;
        }

        /// <summary>
        /// User xem tất cả sự kiện (Hiện tại, Quá khứ, Tương lai)
        /// Đối với User, chúng ta nên lọc chỉ hiện những Event có status là Active hoặc Completed, Draft thì không hiện.
        /// </summary>
        public async Task<IEnumerable<EventListDto>> GetAllEventsForUserAsync()
        {
            var allEvents = await _eventRepo.GetAllAsync();

            return allEvents
                .Where(e => e.Status != "Draft") // Admin mới thấy Draft
                .Select(e => new EventListDto
                {
                    EventId = e.EventId,
                    Title = e.Title,
                    Description = e.Description,
                    Status = e.Status,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    CreatorName = e.Creator?.UserName,
                    ParticipantCount = e.Posts?.Count ?? 0
                });
        }

        /// <summary>
        /// Lấy chi tiết một Event kèm theo các Prize liên quan
        /// </summary>
        public async Task<EventDetailDto?> GetEventDetailsAsync(int eventId)
        {
            var e = await _eventRepo.GetByIdAsync(eventId); // Đảm bảo Repo đã .Include PrizeEvents và EventExperts
            if (e == null) return null;

            return new EventDetailDto
            {
                EventId = e.EventId,
                Title = e.Title,
                Description = e.Description,
                ExpertWeight = e.ExpertWeight,
                UserWeight = e.UserWeight,
                AppliedFee = e.AppliedFee,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                Status = e.Status,
                CreatorId = e.CreatorId,
                CreatorName = e.Creator?.UserName,

                // Map List Prize
                Prizes = e.PrizeEvents.Select(p => new PrizeDtoV1
                {
                    PrizeEventId = p.PrizeEventId,
                    Ranked = p.Ranked,
                    RewardAmount = p.RewardAmount,
                    Status = p.Status
                }).OrderBy(p => p.Ranked).ToList(),

                // Map List Experts
                Experts = e.EventExperts.Select(ex => new ExpertInEventDto
                {
                    ExpertId = ex.ExpertId,
                    FullName = ex.Expert?.UserName,
                    // Nếu có Include ExpertProfile thì lấy thêm trường chuyên môn
                }).ToList()
            };
        }

        /// <summary>
        /// Lấy danh sách bài post tham gia trong một Event để Expert thực hiện chấm điểm
        /// </summary>
        public async Task<IEnumerable<Post>> GetPostsByEventIdAsync(int eventId)
        {
            return await _postRepo.GetPostsByEventIdAsync(eventId);
        }

        public async Task<IEnumerable<Event>> GetExpertEventsAsync(int expertId) => await _eventRepo.GetAllByCreatorIdAsync(expertId);

        public async Task<AnalyticsDashboardResponse> GetAnalyticsAsync(string period)
        {
            int creatorId = _currentUserService.GetRequiredUserId();
            DateTime startDate = period == "90d" ? DateTime.UtcNow.AddDays(-90) : DateTime.UtcNow.AddDays(-30);

            // 1. Lấy dữ liệu từ Repo (Ép kiểu về List để tránh lỗi Count/IEnumerable)
            var eventsResult = await _eventRepo.GetAnalyticsDataAsync(creatorId, startDate);
            var events = eventsResult.ToList();
            var allPosts = events.SelectMany(e => e.Posts).ToList();

            // 2. Tính toán Stats
            var totalEvents = events.Count;

            // FIX: Ép kiểu long/int rõ ràng để tránh lỗi Inference ở hàm Sum
            int totalReach = allPosts.Sum(p => (p.LikeCount ?? 0) + (p.CommentCount ?? 0) + (p.ShareCount ?? 0));
            var activeAttendees = allPosts.Select(p => p.AccountId).Distinct().Count();

            // FIX: Tương tự cho interactions
            int totalInteractions = allPosts.Sum(p =>
                (p.Scoreboard?.FinalLikeCount ?? 0) + (p.Scoreboard?.FinalShareCount ?? 0));

            double engagementRate = totalReach > 0
                ? (double)totalInteractions / totalReach * 100
                : 0;

            // 3. Xử lý Top Events (Dùng dấu ngoặc nhọn để định nghĩa Delegate rõ ràng)
            var topEvents = events
                .OrderByDescending(e => e.Posts.Count)
                .Take(3)
                .Select(e => {
                    int eventReach = e.Posts.Sum(p => (p.LikeCount ?? 0) + (p.CommentCount ?? 0) + (p.ShareCount ?? 0));
                    return new TopEventDto
                    {
                        Id = e.EventId,
                        Title = e.Title,
                        Views = eventReach >= 1000 ? $"{(eventReach / 1000.0):F1}K" : eventReach.ToString(),
                        Progress = CalculateProgress(e)
                    };
                }).ToList();

            // 4. Chart Data (FIX: Nullable DateTime và OrderBy)
            var chartData = allPosts
                .Where(p => p.CreatedAt.HasValue)
                .GroupBy(p => p.CreatedAt!.Value.ToString("MMM dd"))
                .Select(g => new ChartDataDto
                {
                    Name = g.Key,
                    Value = g.Count()
                })
                .ToList();

            return new AnalyticsDashboardResponse
            {
                Stats = MapToStats(totalReach, activeAttendees, engagementRate, totalEvents),
                TopEvents = topEvents,
                ChartData = chartData
            };
        }

        private List<StatCardDto> MapToStats(int reach, int attendees, double rate, int events)
        {
            return new List<StatCardDto>
    {
        new() { Label = "Total Reach", Value = reach >= 1000 ? $"{(reach/1000.0):F1}K" : reach.ToString(), Change = "+12%", IsUp = true },
        new() { Label = "Active Attendees", Value = attendees.ToString(), Change = "+5%", IsUp = true },
        new() { Label = "Engagement Rate", Value = $"{rate:F1}%", Change = "-2.1%", IsUp = false },
        new() { Label = "Total Events", Value = events.ToString(), Change = "+2", IsUp = true }
    };
        }

        private int CalculateProgress(Event e)
        {
            // Ví dụ: tính % hoàn thành dựa trên mục tiêu 50 bài post mỗi event
            int goal = 50;
            int current = e.Posts.Count;
            return Math.Min((current * 100) / goal, 100);
        }
        #endregion
    }
}