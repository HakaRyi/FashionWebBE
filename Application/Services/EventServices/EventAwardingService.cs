using Application.Interfaces;
using Application.Services.NotificationImp;
using Application.Utils.File;
using Domain.Constants;
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
            if (ev == null) throw new Exception("The event does not exist.");

            if (ev.Status != "Active" && ev.Status != "Judging")
                throw new Exception($"The award cannot be given. Current status: {ev.Status}");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var rankedPosts = await CalculateAndRankPostsAsync(eventId, ev);

                var allEscrows = await _escrowRepo.GetTotalEscrowsByEventIdAsync(eventId);

                var prizeEscrow = allEscrows.FirstOrDefault(e => e.Status == EscrowStatus.Held
                                                              && e.Description != null
                                                              && e.Description.StartsWith("PRIZE_POOL"));

                if (prizeEscrow == null) throw new Exception("No valid prize pool deposit was found for this event.");

                var feeEscrows = allEscrows.Where(e => e.Status == EscrowStatus.Held
                                                    && (e.Description == null || !e.Description.StartsWith("PRIZE_POOL")))
                                           .ToList();

                // 3. Trao giải cho người thắng từ Quỹ giải thưởng
                decimal totalDistributedAmount = await DistributePrizesAsync(eventId, ev, rankedPosts);

                // 4. Xử lý hoàn tiền ký quỹ thừa cho Creator
                await RefundRemainingEscrowAsync(ev, prizeEscrow, totalDistributedAmount);

                // 5. GIẢI NGÂN TIỀN PHÍ THAM GIA THỰC TẾ CHO CREATOR
                await ReleaseEventRevenueToCreatorAsync(ev, feeEscrows);

                // 6. Đánh giá và trừ điểm chuyên gia
                await EvaluateExpertPerformanceAsync(eventId, ev);

                // 7. Đóng sự kiện và dọn dẹp Quartz Job
                await CloseEventAndCleanupAsync(ev);

                // CHỐT GIAO DỊCH AN TOÀN
                await _unitOfWork.CommitAsync();

                try
                {
                    var scheduler = await _schedulerFactory.GetScheduler();
                    var jobKeyFinalize = new JobKey($"Job_Finalize_{ev.EventId}", "EventAwardGroup");
                    if (await scheduler.CheckExists(jobKeyFinalize))
                    {
                        await scheduler.DeleteJob(jobKeyFinalize);
                    }
                }
                catch (Exception) {  }
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception($"Error during the event's closing and awards ceremony: {ex.Message}");
            }
        }

        private async Task<List<Post>> CalculateAndRankPostsAsync(int eventId, Event ev)
        {
            var allPosts = await _postRepo.GetPostsByEventIdAsync(eventId);
            if (!allPosts.Any()) return new List<Post>();

            var allRatings = await _ratingRepo.GetAllRatingsByEventIdAsync(eventId);
            var ratingsLookup = allRatings.ToLookup(r => r.PostId);

            double maxRawScore = await _postRepo.GetMaxRawCommunityScoreAsync(eventId, ev.PointPerLike, ev.PointPerShare);
            if (maxRawScore == 0) maxRawScore = 1;

            double totalWeight = ev.ExpertWeight + ev.UserWeight;

            foreach (var post in allPosts)
            {
                double currentRaw = (post.LikeCount ?? 0) * ev.PointPerLike + (post.ShareCount ?? 0) * ev.PointPerShare;
                double normalizedCommunityScore = (currentRaw / maxRawScore) * 10;


                var postRatings = ratingsLookup[post.PostId].ToList();
                double avgExpertScore = postRatings.Any() ? postRatings.Average(r => r.Score) : 0;

                double rawFinalScore = totalWeight > 0
                    ? (avgExpertScore * ev.ExpertWeight + normalizedCommunityScore * ev.UserWeight) / totalWeight
                    : 0;
                double finalScore = Math.Round(rawFinalScore, 3);

                var sb = await _scoreboardRepo.GetByPostIdAsync(post.PostId) ?? new Scoreboard { CreatedAt = DateTime.UtcNow };
                sb.PostId = post.PostId;
                sb.ExpertScore = avgExpertScore;
                sb.CommunityScore = normalizedCommunityScore;
                sb.FinalScore = finalScore;
                sb.FinalLikeCount = post.LikeCount ?? 0;
                sb.FinalShareCount = post.ShareCount ?? 0;
                sb.Status = "Completed";

                if (sb.ScoreboardId == 0) await _scoreboardRepo.AddAsync(sb);
                else _scoreboardRepo.Update(sb);

                post.Scoreboard = sb;
            }

            await _unitOfWork.SaveChangesAsync();

            return allPosts
                .OrderByDescending(p => p.Scoreboard!.FinalScore)
                .ThenBy(p => p.CreatedAt)
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
                if (winnerWallet == null) throw new Exception($"User {post.AccountId}'s wallet could not be found.");

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
                    Description = $"Prize award for {prize.Ranked} place in event '{ev.Title}'",
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
                        Description = $"Refund of surplus escrow from event '{ev.Title}' due to insufficient winners.",
                        Status = "Success",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            escrow.Status = EscrowStatus.Released;
            escrow.ResolvedAt = DateTime.UtcNow;
            escrow.Description = $"Disbursed {totalDistributedAmount:N0} to winners. Refunded {refundAmount:N0} to creator.";
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

            await _unitOfWork.SaveChangesAsync();
        }

        private async Task EvaluateExpertPerformanceAsync(int eventId, Event ev)
        {
            var allPosts = await _postRepo.GetPostsByEventIdAsync(eventId);
            int totalPosts = allPosts.Count();

            if (totalPosts == 0) return;

            var eventExperts = await _eventExpertRepo.GetByEventIdAsync(eventId);
            var acceptedExperts = eventExperts.Where(e => e.Status == "Accepted").ToList();

            if (!acceptedExperts.Any()) return;

            var allRatings = await _ratingRepo.GetAllRatingsByEventIdAsync(eventId);
            var ratingsByExpert = allRatings.ToLookup(r => r.ExpertId);

            // 4. Bắt đầu đánh giá từng chuyên gia
            foreach (var eventExpert in acceptedExperts)
            {
                var expertProfile = await _profileRepo.GetByAccountIdAsync(eventExpert.ExpertId);
                if (expertProfile == null) continue;

                int ratedCount = ratingsByExpert[expertProfile.AccountId].Count();
                int missingCount = totalPosts - ratedCount;

                int currentScore = expertProfile.ReputationScore ?? 100;
                int pointChange = 0;
                string reason = "";
                bool isUpdated = false;

                // --- LOGIC CỘNG ĐIỂM (Plus Score) ---
                if (missingCount <= 0)
                {
                    if (currentScore < 100)
                    {
                        int bonusPoints = 5;
                        int newScore = Math.Min(100, currentScore + bonusPoints);
                        pointChange = newScore - currentScore;
                        reason = $"Completed grading 100% of the exam papers during the event: '{ev.Title}'. (Reputation score restoration)";

                        expertProfile.ReputationScore = newScore;
                        isUpdated = true;
                    }
                }
                // --- LOGIC TRỪ ĐIỂM (Minus Score) ---
                else
                {
                    double missingPercentage = (double)missingCount / totalPosts * 100;

                    if (missingPercentage <= 10)
                    {
                        pointChange = -2;
                        reason = $"Missed {missingCount} evaluations (under 10%) in the event '{ev.Title}'.";
                    }
                    else if (missingPercentage <= 50)
                    {
                        pointChange = -10;
                        reason = $"Withdrawing from the judging process (missing {missingPercentage:0.0}%) in the event '{ev.Title}'.";
                    }
                    else
                    {
                        pointChange = -20;
                        reason = $"Giving up, failing to fulfill the responsibilities of a judge ({missingPercentage:0.0}% missing) in the event '{ev.Title}'.";
                    }

                    int penalizedScore = Math.Max(0, currentScore + pointChange);
                    expertProfile.ReputationScore = penalizedScore;
                    isUpdated = true;
                }

                if (isUpdated)
                {
                    _profileRepo.Update(expertProfile);

                    await _reputationHistory.AddAsync(new ReputationHistory
                    {
                        ExpertProfileId = expertProfile.ExpertProfileId,
                        PointChange = pointChange,
                        CurrentPoint = expertProfile.ReputationScore.Value,
                        Reason = reason,
                        CreatedAt = DateTime.UtcNow
                    });

                    try
                    {
                        await _notificationService.SendNotificationAsync(new Application.Request.NotificationReq.SendNotificationRequest
                        {
                            SenderId = 1,
                            TargetUserId = expertProfile.AccountId,
                            Title = pointChange >= 0 ? "Update your reputation score (Rewards)" : "Update reputation score (Penalties)",
                            Content = reason,
                            Type = "Reputation_Updated",
                            RelatedId = ev.EventId.ToString()
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending notification to Expert {expertProfile.AccountId}: {ex.Message}");
                    }
                }

                await _unitOfWork.SaveChangesAsync();
            }
        }

        private async Task ReleaseEventRevenueToCreatorAsync(Event ev, List<EscrowSession> feeEscrows)
        {
            if (ev.EntryFee <= 0 || feeEscrows == null || !feeEscrows.Any()) return;

            var creatorWallet = await _walletRepo.GetByAccountIdAsync(ev.CreatorId);
            if (creatorWallet == null) throw new Exception("Organizer's wallet not found during revenue release.");

            decimal totalRevenue = feeEscrows.Sum(e => e.Amount);

            if (totalRevenue > 0)
            {
                decimal balanceBefore = creatorWallet.Balance;

                creatorWallet.Balance += totalRevenue;
                creatorWallet.UpdatedAt = DateTime.UtcNow;
                _walletRepo.Update(creatorWallet);

                foreach (var escrow in feeEscrows)
                {
                    escrow.Status = EscrowStatus.Released;
                    escrow.ResolvedAt = DateTime.UtcNow;
                    _escrowRepo.Update(escrow);
                }

                await _transactionRepo.AddAsync(new Transaction
                {
                    WalletId = creatorWallet.WalletId,
                    TransactionCode = $"REVENUE_RELEASE_{ev.EventId}_{DateTime.UtcNow.Ticks}",
                    Amount = totalRevenue,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = creatorWallet.Balance,
                    Type = "Event_Revenue_Released",
                    Status = "Success",
                    Description = $"Received total revenue from {feeEscrows.Count} participant entry fees for event '{ev.Title}'",
                    ReferenceId = ev.EventId,
                    ReferenceType = "Event",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _unitOfWork.SaveChangesAsync();
        }
    }
}
