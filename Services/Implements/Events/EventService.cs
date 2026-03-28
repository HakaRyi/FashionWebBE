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

                var eventData = dto.Adapt<Event>();

                eventData.MinExpertsToStart = dto.MinExpertsRequired;
                eventData.CreatorId = creatorId;
                eventData.AppliedFee = currentFee;
                eventData.Status = "Pending_Review";
                eventData.CreatedAt = DateTime.UtcNow;

                if (dto.ImageFile != null)
                {
                    string uploadedUrl = await _fileService.UploadAsync(dto.ImageFile);
                    eventData.Images.Add(new Image
                    {
                        ImageUrl = uploadedUrl,
                        OwnerType = "Event_Thumbnail",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _eventRepo.AddAsync(eventData);
                await _unitOfWork.SaveChangesAsync();

                await CreatePrizesAsync(eventData.EventId, dto.Prizes);
                await SetupExpertPanelAsync(eventData.EventId, creatorId, dto.InvitedExpertIds, isDraft: true);

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
            if (ev == null || ev.Status != "Inviting") return;

            var experts = await _eventExpertRepo.GetByEventIdAsync(eventId);
            int acceptedCount = experts.Count(e => e.Status == "Accepted");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var wallet = await _walletRepo.GetByAccountIdAsync(ev.CreatorId);
                var prizesData = await _prizeRepo.GetByEventIdAsync(eventId);
                decimal totalPrizeAmount = prizesData.Sum(p => p.RewardAmount);

                // TRƯỜNG HỢP 1: Thất bại - Không đủ Expert tham gia
                if (acceptedCount < ev.MinExpertsToStart)
                {
                    decimal totalToRefund = totalPrizeAmount + ev.AppliedFee;
                    wallet.LockedBalance -= totalToRefund;
                    wallet.Balance += totalToRefund;

                    ev.Status = "Cancelled_NotEnoughExperts";
                    _eventRepo.Update(ev);
                    _walletRepo.Update(wallet);
                }
                // TRƯỜNG HỢP 2: Thành công - Kích hoạt và Ký quỹ
                else
                {
                    await CollectSystemFeeAsync(wallet, ev);
                    await ProcessEscrowFromLockedAsync(eventId, ev.CreatorId, totalPrizeAmount, wallet);

                    ev.Status = "Active";
                    _eventRepo.Update(ev);

                    await ScheduleEventFinalization(ev);

                }

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

        public async Task ManualStartEventAsync(int eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev == null) throw new Exception("Sự kiện không tồn tại.");

            // 1. Kiểm tra trạng thái
            if (ev.Status != "Inviting")
                throw new Exception("Sự kiện chưa được duyệt hoặc đã bắt đầu/kết thúc.");

            // 2. CHECK GIỚI HẠN THỜI GIAN (Ví dụ: tối đa 24 tiếng)
            double maxEarlyHours = await _settingRepo.GetDoubleValueAsync("MaxEarlyStartHours", 24.0);
            DateTime now = DateTime.Now;

            // Nếu hiện tại cách giờ bắt đầu dự kiến > 24 tiếng -> Chặn
            if ((ev.StartTime.Value - now).TotalHours > maxEarlyHours)
            {
                throw new Exception($"Bạn chỉ có thể bắt đầu sớm tối đa {maxEarlyHours} tiếng so với lịch dự kiến.");
            }

            if (now >= ev.StartTime)
            {
                throw new Exception("Đã đến giờ bắt đầu dự kiến, vui lòng đợi hệ thống tự động kích hoạt trong giây lát.");
            }

            // 3. Kiểm tra số lượng Expert hiện tại
            var experts = await _eventExpertRepo.GetByEventIdAsync(eventId);
            int acceptedCount = experts.Count(e => e.Status == "Accepted");

            if (acceptedCount < ev.MinExpertsToStart)
                throw new Exception($"Chưa đủ số lượng chuyên gia tối thiểu ({ev.MinExpertsToStart}).");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var wallet = await _walletRepo.GetByAccountIdAsync(ev.CreatorId);
                var prizesData = await _prizeRepo.GetByEventIdAsync(eventId);
                decimal totalPrizeAmount = prizesData.Sum(p => p.RewardAmount);

                // 1. Thu phí hệ thống & Chuyển tiền vào Escrow (Ký quỹ)
                await CollectSystemFeeAsync(wallet, ev);
                await ProcessEscrowFromLockedAsync(eventId, ev.CreatorId, totalPrizeAmount, wallet);

                var originalDuration = ev.EndTime.Value - ev.StartTime.Value;

                // 2. Cập nhật thông tin Event: Chuyển sang Active và cập nhật StartTime thực tế
                ev.Status = "Active";
                ev.StartTime = DateTime.Now;
                _eventRepo.Update(ev);

                // 3. Xử lý các Expert chưa phản hồi (Pending) -> Chuyển thành Closed
                foreach (var exp in experts.Where(e => e.Status == "Pending"))
                {
                    exp.Status = "Closed_InvitationExpired";
                    _eventExpertRepo.Update(exp);
                }

                await _unitOfWork.SaveChangesAsync();

                // 4. HỦY BACKGROUND JOB ĐÃ LẬP LỊCH TRƯỚC ĐÓ
                var scheduler = await _schedulerFactory.GetScheduler();
                var jobKey = new JobKey($"Job_Activate_{ev.EventId}", "EventGroup");
                if (await scheduler.CheckExists(jobKey))
                {
                    await scheduler.DeleteJob(jobKey);
                }

                await ScheduleEventFinalization(ev);

                await _unitOfWork.CommitAsync();

            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception($"Lỗi khi bắt đầu sự kiện thủ công: {ex.Message}");
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
            var startTime = ev.StartTime ?? DateTime.Now.AddSeconds(30);

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"Trigger_Activate_{ev.EventId}", "EventGroup")
                .WithDescription($"Lịch kích hoạt cho sự kiện '{ev.Title}' vào lúc {startTime}")
                .StartAt(startTime)
                .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                .Build();

            await scheduler.ScheduleJob(job, trigger);
        }

        private async Task ScheduleEventFinalization(Event ev)
        {
            if (!ev.EndTime.HasValue) return;

            var scheduler = await _schedulerFactory.GetScheduler();

            var job = JobBuilder.Create<FinalizeEventJob>()
                .WithIdentity($"Job_Finalize_{ev.EventId}", "EventAwardGroup")
                .WithDescription($"Tự động trao giải sự kiện: {ev.Title} (ID: {ev.EventId})")
                .UsingJobData("EventId", ev.EventId)
                .Build();

            DateTime processTime = ev.EndTime.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(ev.EndTime.Value, DateTimeKind.Local)
                : ev.EndTime.Value;

            DateTimeOffset endTimeOffset = new DateTimeOffset(processTime);

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"Trigger_Finalize_{ev.EventId}", "EventAwardGroup")
                .WithDescription($"Lịch trao giải sự kiện '{ev.Title}' vào {endTimeOffset:dd/MM/yyyy HH:mm}")
                .StartAt(endTimeOffset)
                // Misfire Instruction: Nếu Server tắt ngay lúc EndTime, khi bật lại sẽ bắn bù ngay!
                .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                .Build();

            await scheduler.ScheduleJob(job, trigger);
        }

        private void ValidateEventRequest(CreateEventRequest dto)
        {
            if (dto.StartTime >= dto.SubmissionDeadline)
            {
                throw new Exception("Ngày bắt đầu phải trước hạn chót nộp bài.");
            }

            if (dto.SubmissionDeadline >= dto.EndTime)
            {
                throw new Exception("Hạn chót nộp bài phải trước ngày kết thúc sự kiện (để Expert có thời gian chấm).");
            }

            if (Math.Abs(dto.ExpertWeight + dto.UserWeight - 1.0) > 0.001)
                throw new Exception("Tổng trọng số Expert và User phải bằng 1.0.");

            if (dto.MinExpertsRequired < 2)
                throw new Exception("Số lượng Expert yêu cầu tối thiểu không được dưới 1.");
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

        private async Task SetupExpertPanelAsync(int eventId, int creatorId, List<int>? invitedIds, bool isDraft)
        {
            var status = isDraft ? "Awaiting_Review" : "Pending";

            var expertPanel = new List<EventExpert> {
                new EventExpert {
                    EventId = eventId,
                    ExpertId = creatorId,
                    JoinedAt = DateTime.Now,
                    Status = "Accepted"
                }
            };

            if (invitedIds != null)
            {
                expertPanel.AddRange(invitedIds.Distinct().Where(id => id != creatorId).Select(id => new EventExpert
                {
                    EventId = eventId,
                    ExpertId = id,
                    JoinedAt = DateTime.Now,
                    Status = status
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
            if (post == null || post.EventId == null) throw new Exception("Bài viết không tồn tại hoặc không thuộc sự kiện nào.");

            var ev = await _eventRepo.GetByIdAsync(post.EventId.Value);
            // Nên kiểm tra thêm trạng thái "Judging" (Đang chấm điểm), vì kết thúc nộp bài mới được chấm
            if (ev == null || (ev.Status != "Active" && ev.Status != "Judging"))
                throw new Exception("Sự kiện không trong thời gian cho phép chấm điểm.");

            var isMember = await _eventExpertRepo.AnyAsync(ee =>
                ee.EventId == post.EventId && ee.ExpertId == currentExpertId && ee.Status == "Accepted");

            if (!isMember) throw new Exception("Bạn không có quyền chấm điểm cho sự kiện này.");

            // Bắt đầu Transaction
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var existingRating = await _ratingRepo.GetByPostAndExpertAsync(dto.PostId, currentExpertId);
                if (existingRating != null)
                {
                    existingRating.Score = dto.Score;
                    existingRating.Reason = dto.Reason;
                    existingRating.UpdatedAt = DateTime.UtcNow; // Ưu tiên dùng UtcNow
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
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                // BẮT BUỘC SAVE ĐỂ HÀM TÍNH TOÁN DƯỚI ĐÂY LẤY ĐƯỢC ĐIỂM MỚI NHẤT
                await _unitOfWork.SaveChangesAsync();

                // CHỈ TÍNH LẠI ĐIỂM CHO ĐÚNG BÀI VIẾT VỪA ĐƯỢC CHẤM (CHỐNG DEADLOCK)
                await UpdateSinglePostScoreboardAsync(post, ev);

                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                // Ghi log lỗi ex.Message ở đây nếu có NLog/Serilog
                throw new Exception($"Lỗi trong quá trình chấm điểm: Lỗi hệ thống.");
            }
        }

        private async Task UpdateSinglePostScoreboardAsync(Post post, Event ev)
        {
            // 1. Tìm điểm Community Max hiện tại của toàn sự kiện (để chuẩn hóa hệ 10)
            double maxRawScore = await _postRepo.GetMaxRawCommunityScoreAsync(ev.EventId, ev.PointPerLike, ev.PointPerShare);
            if (maxRawScore == 0) maxRawScore = 1;

            // 2. Tính điểm cộng đồng của bài viết này (Hệ 10)
            double currentRaw = (post.LikeCount ?? 0) * ev.PointPerLike + (post.ShareCount ?? 0) * ev.PointPerShare;
            double normalizedCommunityScore = (currentRaw / maxRawScore) * 10;

            // 3. Tính điểm chuyên môn
            var ratings = (await _ratingRepo.GetRatingsByPostIdAsync(post.PostId)).ToList();

            // CASE GIẢI QUYẾT 3/5 GIÁM KHẢO: Chia cho số lượng người ĐÃ CHẤM (ratings.Count)
            double avgExpertScore = ratings.Any() ? ratings.Average(r => r.Score) : 0;

            // Lấy tổng số giám khảo của sự kiện để kiểm tra tiến độ
            int totalExpertsInEvent = await _eventExpertRepo.CountAcceptedExpertsAsync(ev.EventId);
            int judgedCount = ratings.Count;

            double totalWeight = ev.ExpertWeight + ev.UserWeight;
            double finalScore = totalWeight > 0
                ? ((avgExpertScore * ev.ExpertWeight) + (normalizedCommunityScore * ev.UserWeight)) / totalWeight
                : 0;

            // 5. Cập nhật hoặc tạo mới Scoreboard
            var sb = await _scoreboardRepo.GetByPostIdAsync(post.PostId);
            if (sb == null)
            {
                await _scoreboardRepo.AddAsync(new Scoreboard
                {
                    PostId = post.PostId,
                    ExpertScore = avgExpertScore,
                    CommunityScore = normalizedCommunityScore,
                    FinalScore = finalScore,
                    FinalLikeCount = post.LikeCount ?? 0,
                    FinalShareCount = post.ShareCount ?? 0,
                    CreatedAt = DateTime.UtcNow,
                    Status = judgedCount >= totalExpertsInEvent ? "Completed" : "Judging"
                });
            }
            else
            {
                sb.ExpertScore = avgExpertScore;
                sb.CommunityScore = normalizedCommunityScore;
                sb.FinalScore = finalScore;
                sb.FinalLikeCount = post.LikeCount ?? 0;
                sb.FinalShareCount = post.ShareCount ?? 0;
                sb.Status = judgedCount >= totalExpertsInEvent ? "Completed" : "Judging";
                _scoreboardRepo.Update(sb);
            }

            await _unitOfWork.SaveChangesAsync();
        }
        #endregion

        #region Kết Thúc Sự Kiện & Trao Giải
        public async Task FinalizeAndAwardEventAsync(int eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev == null) throw new Exception("Sự kiện không tồn tại.");

            // 1. Chỉ trao giải cho sự kiện đang Active hoặc đang ở trạng thái Chờ trao giải (Judging/PendingAward)
            if (ev.Status != "Active" && ev.Status != "Judging")
                throw new Exception($"Không thể trao giải. Trạng thái hiện tại: {ev.Status}");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 2. Lấy danh sách bài viết đã có điểm & Sắp xếp tìm người thắng
                var posts = await _postRepo.GetGradedPostsByEventIdAsync(eventId);

                // LOGIC TÌM NGƯỜI THẮNG (TIE-BREAKER): Điểm cao xếp trước, nếu bằng điểm thì ai nộp trước xếp trước
                var rankedPosts = posts
                    .OrderByDescending(p => p.Scoreboard!.FinalScore)
                    .ThenBy(p => p.CreatedAt)
                    .ToList();

                // 3. Lấy cấu trúc giải thưởng của sự kiện
                var prizes = (await _prizeRepo.GetByEventIdAsync(eventId))
                    .OrderBy(p => p.Ranked)
                    .ToList();

                // 4. Lấy phiên ký quỹ (Escrow) để chuẩn bị giải ngân
                var escrow = await _escrowRepo.GetActiveEscrowByEventIdAsync(eventId);
                if (escrow == null) throw new Exception("Không tìm thấy khoản ký quỹ hợp lệ cho sự kiện này.");

                decimal totalDistributedAmount = 0;

                // 5. Bắt đầu trao giải (Khớp số lượng bài nộp với số lượng giải)
                int winningCount = Math.Min(rankedPosts.Count, prizes.Count);

                for (int i = 0; i < winningCount; i++)
                {
                    var post = rankedPosts[i];
                    var prize = prizes[i];
                    var winnerAccountId = post.AccountId;
                    decimal rewardAmount = prize.RewardAmount;

                    // 5.1 Lưu thông tin người thắng giải vào DB
                    var winnerRecord = new EventWinner
                    {
                        AccountId = winnerAccountId,
                        PrizeEventId = prize.PrizeEventId,
                        WinningScore = post.Scoreboard!.FinalScore,
                        FinalRank = prize.Ranked,
                        Status = "Awarded",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _winnerRepo.AddAsync(winnerRecord);

                    // Cập nhật trạng thái PrizeEvent
                    prize.Status = "Awarded";
                    _prizeRepo.Update(prize);

                    // 5.2 Giải ngân: Cộng tiền vào ví người thắng
                    var winnerWallet = await _walletRepo.GetByAccountIdAsync(winnerAccountId);
                    if (winnerWallet == null) throw new Exception($"Ví của người dùng {winnerAccountId} không tồn tại.");

                    decimal balanceBefore = winnerWallet.Balance;
                    winnerWallet.Balance += rewardAmount;
                    winnerWallet.UpdatedAt = DateTime.UtcNow;
                    _walletRepo.Update(winnerWallet);

                    // 5.3 Lưu lịch sử giao dịch (Transaction)
                    await _transactionRepo.AddAsync(new Transaction
                    {
                        WalletId = winnerWallet.WalletId,
                        Amount = rewardAmount,
                        BalanceBefore = balanceBefore,
                        BalanceAfter = winnerWallet.Balance,
                        Type = "Prize_Reward",
                        ReferenceId = eventId,
                        ReferenceType = "Event",
                        Description = $"Nhận thưởng Giải {prize.Ranked} sự kiện '{ev.Title}'",
                        Status = "Success",
                        CreatedAt = DateTime.UtcNow
                    });

                    totalDistributedAmount += rewardAmount;
                }

                // 6. XỬ LÝ SỐ TIỀN THỪA VÀ HOÀN TRẢ (QUAN TRỌNG)
                // Nếu số người tham gia hợp lệ ÍT HƠN số giải thưởng, tiền thừa phải trả về cho Creator
                decimal refundAmount = escrow.Amount - totalDistributedAmount;
                if (refundAmount > 0)
                {
                    var creatorWallet = await _walletRepo.GetByAccountIdAsync(ev.CreatorId);
                    if (creatorWallet != null)
                    {
                        decimal creatorBalanceBefore = creatorWallet.Balance;
                        creatorWallet.Balance += refundAmount;
                        creatorWallet.UpdatedAt = DateTime.UtcNow;
                        _walletRepo.Update(creatorWallet);

                        await _transactionRepo.AddAsync(new Transaction
                        {
                            WalletId = creatorWallet.WalletId,
                            Amount = refundAmount,
                            BalanceBefore = creatorBalanceBefore,
                            BalanceAfter = creatorWallet.Balance,
                            Type = "Event_Refund",
                            ReferenceId = eventId,
                            ReferenceType = "Event",
                            Description = $"Hoàn tiền ký quỹ dư từ sự kiện '{ev.Title}' do không đủ người đạt giải.",
                            Status = "Success",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                // 7. Tất toán Escrow
                escrow.Status = "Resolved";
                escrow.ResolvedAt = DateTime.UtcNow;
                escrow.Description = $"Đã giải ngân {totalDistributedAmount:N0}. Hoàn trả {refundAmount:N0}.";
                _escrowRepo.Update(escrow);

                // 8. Đóng sự kiện
                ev.Status = "Completed";
                ev.EndTime = DateTime.UtcNow; // Cập nhật lại thời gian kết thúc thực tế
                _eventRepo.Update(ev);

                // Lưu tất cả thay đổi
                await _unitOfWork.SaveChangesAsync();

                var scheduler = await _schedulerFactory.GetScheduler();
                var jobKeyFinalize = new JobKey($"Job_Finalize_{ev.EventId}", "EventAwardGroup");
                if (await scheduler.CheckExists(jobKeyFinalize))
                {
                    await scheduler.DeleteJob(jobKeyFinalize);
                }

                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception($"Lỗi trong quá trình kết thúc và trao giải sự kiện: {ex.Message}");
            }
        }
        #endregion

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

                await ScheduleEventActivation(ev);
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

        #region Logic Truy vấn (Get Methods)
        public async Task<IEnumerable<PostReviewDto>> GetPostsForReviewAsync(int eventId)
        {
            int currentExpertId = _currentUserService.GetRequiredUserId();

            // Đảm bảo PostRepo.GetPostsByEventIdAsync đã .Include(p => p.ExpertRatings)
            var posts = await _postRepo.GetPostsByEventIdAsync(eventId);

            return posts.Select(p => {
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
            var e = await _eventRepo.GetByIdAsync(eventId);
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
                SubmissionDeadline = e.SubmissionDeadline,
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
                    Status = ex.Status,
                }).ToList()
            };
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
    }
}