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

                eventData.Status = "Pending_Review";

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
                ev.StartTime = DateTime.Now; // ĐIỂM QUAN TRỌNG: Cập nhật lại thời gian bắt đầu thực tế
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
                SubmissionDeadline = dto.SubmissionDeadline,
                EndTime = dto.EndTime,
                CreatedAt = DateTime.Now,
                MinExpertsToStart = dto.MinExpertsRequired,
                Status = "Pending_Review"
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
            // Cần Include PrizeEvents để tính tổng tiền giải thưởng
            var ev = await _eventRepo.GetByIdAsync(eventId);

            if (ev == null)
                throw new KeyNotFoundException("Không tìm thấy sự kiện.");

            if (ev.Status != "Pending_Review")
                throw new InvalidOperationException("Chỉ có thể từ chối sự kiện đang ở trạng thái 'Chờ duyệt'.");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Vui lòng cung cấp lý do từ chối.");

            // 2. Bắt đầu Transaction để đảm bảo an toàn tài chính
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 3. Tính toán số tiền cần hoàn lại
                // Refund = Phí áp dụng + Tổng các giải thưởng đã nạp
                decimal totalPrizePool = ev.PrizeEvents?.Sum(p => p.RewardAmount) ?? 0;
                decimal totalToRefund = totalPrizePool + ev.AppliedFee;

                // 4. Cập nhật Ví của Creator
                var wallet = await _walletRepo.GetByAccountIdAsync(ev.CreatorId);
                if (wallet == null)
                    throw new Exception("Không tìm thấy ví của người tạo sự kiện.");

                if (wallet.LockedBalance < totalToRefund)
                    throw new Exception("Số dư bị khóa không đủ để thực hiện hoàn tiền (Lỗi logic dữ liệu).");

                // Chuyển tiền từ 'Bị khóa' về lại 'Số dư khả dụng'
                wallet.LockedBalance -= totalToRefund;
                wallet.Balance += totalToRefund;

                // 5. Cập nhật trạng thái sự kiện
                ev.Status = "Rejected";
                ev.Note = reason;

                // 6. Lưu thay đổi
                _eventRepo.Update(ev);
                _walletRepo.Update(wallet);

                // Lưu xuống DB
                await _unitOfWork.SaveChangesAsync();

                // Xác nhận hoàn tất giao dịch
                await _unitOfWork.CommitAsync();

                // TODO: Gửi Notification cho Creator ở đây (nếu có hệ thống thông báo)
            }
            catch (Exception ex)
            {
                // Nếu có bất kỳ lỗi nào, rollback lại toàn bộ để tránh mất tiền
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

            return events.Select(MapToEventListDto);
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