using Application.Interfaces;
using Application.Services.NotificationImp;
using Application.Utils.File;
using Domain.Entities;
using Domain.Interfaces;
using Quartz;
using System.Linq;

namespace Application.Services.EventServices
{
    public class EventAwardingService : IEventAwardingService
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
        private readonly IReputationHistoryRepository _reputationHistory;
        private readonly IExpertProfileRepository _profileRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IImageRepository _imageRepo;
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly ISystemSettingRepository _settingRepo;
        private readonly INotificationService _notificationService;


        public EventAwardingService(
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
            IReputationHistoryRepository reputationHistory,
            IExpertProfileRepository profileRepo,
            IUnitOfWork unitOfWork,
            IImageRepository imageRepo,
            ISystemSettingRepository settingRepo,
            INotificationService notificationService,
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
            _reputationHistory = reputationHistory;
            _profileRepo = profileRepo;
            _settingRepo = settingRepo;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _imageRepo = imageRepo;
            _schedulerFactory = schedulerFactory;
            _notificationService = notificationService;
        }

        public async Task FinalizeAndAwardEventAsync(int eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev == null) throw new Exception("Sự kiện không tồn tại.");

            if (ev.Status != "Active" && ev.Status != "Judging")
                throw new Exception($"Không thể trao giải. Trạng thái hiện tại: {ev.Status}");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 1. Tính toán điểm cho tất cả bài viết và lấy danh sách đã xếp hạng
                var rankedPosts = await CalculateAndRankPostsAsync(eventId, ev);

                // 2. Lấy thông tin Escrow
                var escrow = await _escrowRepo.GetActiveEscrowByEventIdAsync(eventId);
                if (escrow == null) throw new Exception("Không tìm thấy khoản ký quỹ hợp lệ cho sự kiện này.");

                // 3. Trao giải cho người thắng (Trả về tổng số tiền đã phát)
                decimal totalDistributedAmount = await DistributePrizesAsync(eventId, ev, rankedPosts);

                // 4. Xử lý hoàn tiền ký quỹ (nếu số người thắng ít hơn số giải)
                await RefundRemainingEscrowAsync(ev, escrow, totalDistributedAmount);

                // 5. ĐÁNH GIÁ VÀ TRỪ ĐIỂM CHUYÊN GIA
                await EvaluateExpertPerformanceAsync(eventId, ev);

                // 6. Đóng sự kiện và dọn dẹp Quartz Job
                await CloseEventAndCleanupAsync(ev);

                // CHỐT GIAO DỊCH
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception($"Lỗi trong quá trình kết thúc và trao giải sự kiện: {ex.Message}");
            }
        }

        private async Task<List<Post>> CalculateAndRankPostsAsync(int eventId, Event ev)
        {
            var allPosts = await _postRepo.GetPostsByEventIdAsync(eventId);
            if (!allPosts.Any()) return new List<Post>();

            double maxRawScore = await _postRepo.GetMaxRawCommunityScoreAsync(eventId, ev.PointPerLike, ev.PointPerShare);
            if (maxRawScore == 0) maxRawScore = 1;

            int totalExpertsInEvent = await _eventExpertRepo.CountAcceptedExpertsAsync(eventId);
            if (totalExpertsInEvent == 0) totalExpertsInEvent = 1;

            double totalWeight = ev.ExpertWeight + ev.UserWeight;

            foreach (var post in allPosts)
            {
                double currentRaw = (post.LikeCount ?? 0) * ev.PointPerLike + (post.ShareCount ?? 0) * ev.PointPerShare;
                double normalizedCommunityScore = currentRaw / maxRawScore * 10;

                var ratings = (await _ratingRepo.GetRatingsByPostIdAsync(post.PostId)).ToList();
                double avgExpertScore = ratings.Sum(r => r.Score) / totalExpertsInEvent;

                double rawFinalScore = totalWeight > 0
                    ? (avgExpertScore * ev.ExpertWeight + normalizedCommunityScore * ev.UserWeight) / totalWeight
                    : 0;
                double finalScore = Math.Round(rawFinalScore, 3);

                var sb = await _scoreboardRepo.GetByPostIdAsync(post.PostId) ?? new Scoreboard();

                sb.PostId = post.PostId;
                sb.ExpertScore = avgExpertScore;
                sb.CommunityScore = normalizedCommunityScore;
                sb.FinalScore = finalScore;
                sb.FinalLikeCount = post.LikeCount ?? 0;
                sb.FinalShareCount = post.ShareCount ?? 0;
                sb.Status = "Completed";

                if (sb.ScoreboardId == 0)
                {
                    sb.CreatedAt = DateTime.UtcNow;
                    await _scoreboardRepo.AddAsync(sb);
                }
                else
                {
                    _scoreboardRepo.Update(sb);
                }

                post.Scoreboard = sb; // Gán vào RAM để lát sort
            }

            await _unitOfWork.SaveChangesAsync(); // Lưu bảng điểm xuống DB

            // Trả về danh sách đã sắp xếp (Bỏ qua bước query lại từ DB)
            return allPosts
                .OrderByDescending(p => p.Scoreboard!.FinalScore)
                .ThenBy(p => p.CreatedAt) // Điểm bằng nhau thì ai nộp trước người đó thắng
                .ToList();
        }

        private async Task<decimal> DistributePrizesAsync(int eventId, Event ev, List<Post> rankedPosts)
        {
            var prizes = (await _prizeRepo.GetByEventIdAsync(eventId)).OrderBy(p => p.Ranked).ToList();
            decimal totalDistributedAmount = 0;

            int winningCount = Math.Min(rankedPosts.Count, prizes.Count);

            for (int i = 0; i < winningCount; i++)
            {
                var post = rankedPosts[i];
                var prize = prizes[i];
                decimal rewardAmount = prize.RewardAmount;

                // 1. Lưu EventWinner
                await _winnerRepo.AddAsync(new EventWinner
                {
                    AccountId = post.AccountId,
                    PrizeEventId = prize.PrizeEventId,
                    WinningScore = post.Scoreboard!.FinalScore,
                    FinalRank = prize.Ranked,
                    Status = "Awarded",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                // 2. Cập nhật Prize
                prize.Status = "Awarded";
                _prizeRepo.Update(prize);

                // 3. Cộng tiền vào ví
                var winnerWallet = await _walletRepo.GetByAccountIdAsync(post.AccountId);
                if (winnerWallet == null) throw new Exception($"Ví của người dùng {post.AccountId} không tồn tại.");

                decimal balanceBefore = winnerWallet.Balance;
                winnerWallet.Balance += rewardAmount;
                winnerWallet.UpdatedAt = DateTime.UtcNow;
                _walletRepo.Update(winnerWallet);

                // 4. Lưu Transaction
                await _transactionRepo.AddAsync(new Transaction
                {
                    WalletId = winnerWallet.WalletId,
                    TransactionCode = $"RW-{eventId}-{post.AccountId}-{DateTime.UtcNow.Ticks}",
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

            await _unitOfWork.SaveChangesAsync();
            return totalDistributedAmount;
        }

        private async Task RefundRemainingEscrowAsync(Event ev, EscrowSession escrow, decimal totalDistributedAmount)
        {
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
                        TransactionCode = $"RF-{ev.EventId}-{DateTime.UtcNow.Ticks}",
                        Amount = refundAmount,
                        BalanceBefore = creatorBalanceBefore,
                        BalanceAfter = creatorWallet.Balance,
                        Type = "Event_Refund",
                        ReferenceId = ev.EventId,
                        ReferenceType = "Event",
                        Description = $"Hoàn tiền ký quỹ dư từ sự kiện '{ev.Title}' do không đủ người đạt giải.",
                        Status = "Success",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // Tất toán Escrow
            escrow.Status = "Resolved";
            escrow.ResolvedAt = DateTime.UtcNow;
            escrow.Description = $"Đã giải ngân {totalDistributedAmount:N0}. Hoàn trả {refundAmount:N0}.";
            _escrowRepo.Update(escrow);

            await _unitOfWork.SaveChangesAsync();
        }

        private async Task CloseEventAndCleanupAsync(Event ev)
        {
            ev.Status = "Completed";
            ev.EndTime = DateTime.UtcNow;
            _eventRepo.Update(ev);

            var eventExperts = await _eventExpertRepo.GetByEventIdAsync(ev.EventId);
            var acceptedExperts = eventExperts.Where(e => e.Status == "Accepted").ToList();

            // 2. Gửi thông báo cho từng Expert
            foreach (var exp in acceptedExperts)
            {
                await _notificationService.SendNotificationAsync(new Application.Request.NotificationReq.SendNotificationRequest
                {
                    SenderId = ev.CreatorId,
                    TargetUserId = exp.ExpertId,
                    Title = "The event has ended.",
                    Content = $"The event '{ev.Title}', where you served as a judge, has successfully concluded. Thank you for your contribution.",
                    Type = "Event_Completed",
                    RelatedId = ev.EventId.ToString()
                });
            }

            var scheduler = await _schedulerFactory.GetScheduler();
            var jobKeyFinalize = new JobKey($"Job_Finalize_{ev.EventId}", "EventAwardGroup");

            if (await scheduler.CheckExists(jobKeyFinalize))
            {
                await scheduler.DeleteJob(jobKeyFinalize);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        private async Task EvaluateExpertPerformanceAsync(int eventId, Event ev)
        {
            // 1. Lấy tổng số bài viết trong sự kiện
            var allPosts = await _postRepo.GetPostsByEventIdAsync(eventId);
            int totalPosts = allPosts.Count();

            if (totalPosts == 0) return;

            // 2. Lấy danh sách chuyên gia ĐÃ ĐỒNG Ý tham gia sự kiện
            var eventExperts = await _eventExpertRepo.GetByEventIdAsync(eventId);
            var acceptedExperts = eventExperts.Where(e => e.Status == "Accepted").ToList();

            if (!acceptedExperts.Any()) return;

            // 3. Lấy tất cả lượt đánh giá của các bài viết trong sự kiện này
            var postIds = allPosts.Select(p => p.PostId).ToList();
            var allRatingsInEvent = new List<ExpertRating>();

            foreach (var postId in postIds)
            {
                var ratings = await _ratingRepo.GetRatingsByPostIdAsync(postId);
                allRatingsInEvent.AddRange(ratings);
            }

            // 4. Bắt đầu đánh giá từng chuyên gia
            foreach (var eventExpert in acceptedExperts)
            {
                var expertProfile = await _profileRepo.GetByAccountIdAsync(eventExpert.ExpertId);
                if (expertProfile == null) continue;

                int ratedCount = allRatingsInEvent.Count(r => r.ExpertId == expertProfile.AccountId);
                int missingCount = totalPosts - ratedCount;

                int currentScore = expertProfile.ReputationScore ?? 100;

                //plus score
                if (missingCount <= 0)
                {
                    if (currentScore < 100)
                    {
                        int bonusPoints = 5;
                        int newScore = Math.Min(100, currentScore + bonusPoints);
                        int actualPointAdded = newScore - currentScore;

                        expertProfile.ReputationScore = newScore;
                        _profileRepo.Update(expertProfile);

                        await _reputationHistory.AddAsync(new ReputationHistory
                        {
                            ExpertProfileId = expertProfile.ExpertProfileId,
                            PointChange = actualPointAdded,
                            CurrentPoint = newScore,
                            Reason = $"Hoàn thành chấm 100% bài thi trong sự kiện '{ev.Title}'. (Phục hồi điểm uy tín)",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    continue;
                }

                // minus score
                double missingPercentage = (double)missingCount / totalPosts * 100;

                int penaltyPoint = 0;
                string penaltyReason = "";

                if (missingPercentage <= 10)
                {
                    penaltyPoint = -2;
                    penaltyReason = $"Missed {missingCount} evaluations (under 10%) in the event '{ev.Title}'.";
                }
                else if (missingPercentage <= 50)
                {
                    penaltyPoint = -10;
                    penaltyReason = $"Withdrawing from the judging process (missing {missingPercentage:0.0}%) in the event '{ev.Title}'.";
                }
                else
                {
                    penaltyPoint = -20;
                    penaltyReason = $"Giving up, failing to fulfill the responsibilities of a judge ({missingPercentage:0.0}% missing) in the event '{ev.Title}'.";
                }

                // Cập nhật điểm (Không để rớt xuống dưới 0)
                int penalizedScore = Math.Max(0, currentScore + penaltyPoint);
                expertProfile.ReputationScore = penalizedScore;

                _profileRepo.Update(expertProfile);

                await _reputationHistory.AddAsync(new ReputationHistory
                {
                    ExpertProfileId = expertProfile.ExpertProfileId,
                    PointChange = penaltyPoint, // Lưu số âm
                    CurrentPoint = penalizedScore,
                    Reason = penaltyReason,
                    CreatedAt = DateTime.UtcNow
                });

                await _notificationService.SendNotificationAsync(new Application.Request.NotificationReq.SendNotificationRequest
                {
                    SenderId = 1,
                    TargetUserId = expertProfile.AccountId,
                    Title = penaltyPoint >= 0 ? "Update your reputation score (Rewards)" : "Update reputation score (Penalties)",
                    Content = penaltyReason,
                    Type = "Reputation_Updated",
                    RelatedId = ev.EventId.ToString()
                });
            }

            await _unitOfWork.SaveChangesAsync();
        }
    }
}
