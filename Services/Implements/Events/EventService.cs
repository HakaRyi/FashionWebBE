using Repositories.Entities;
using Repositories.Repos.EscrowSessionRepos;
using Repositories.Repos.EventExpertRepos;
using Repositories.Repos.Events;
using Repositories.Repos.EventWinnerRepos;
using Repositories.Repos.ExpertRatingRepos;
using Repositories.Repos.PostRepos;
using Repositories.Repos.PrizeEventRepos;
using Repositories.Repos.ScoreboardRepos;
using Repositories.Repos.TransactionRepos;
using Repositories.Repos.WalletRepos;
using Repositories.UnitOfWork;
using Services.Implements.Auth;
using Services.Request.EventReq;
using Services.Request.ExpertRatingReq;

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
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        #region Quản lý Sự kiện và Ký quỹ
        public async Task<Event> CreateEventAndLockFundsAsync(CreateEventRequest dto)
        {
            int expertId = _currentUserService.GetRequiredUserId();

            if (Math.Abs(dto.ExpertWeight + dto.UserWeight - 1.0) > 0.001)
                throw new Exception("Tổng trọng số Expert và User phải bằng 1.0.");

            decimal totalPrizeMoney = dto.Prizes.Sum(p => p.RewardAmount);

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var wallet = await _walletRepo.GetByAccountIdAsync(expertId);
                if (wallet == null || wallet.Balance < totalPrizeMoney)
                    throw new Exception("Số dư ví không đủ để ký quỹ giải thưởng.");

                var eventData = new Event
                {
                    Title = dto.Title,
                    Description = dto.Description,
                    CreatorId = expertId,
                    ExpertWeight = dto.ExpertWeight,
                    UserWeight = dto.UserWeight,
                    PointPerLike = dto.PointPerLike,
                    PointPerShare = dto.PointPerShare,
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime,
                    CreatedAt = DateTime.Now,
                    Status = "Active"
                };
                await _eventRepo.AddAsync(eventData);
                await _unitOfWork.SaveChangesAsync();

                var prizes = dto.Prizes.Select(p => new PrizeEvent
                {
                    EventId = eventData.EventId,
                    Ranked = p.Ranked,
                    RewardAmount = p.RewardAmount,
                    Status = "Active"
                }).ToList();
                await _prizeRepo.AddRangeAsync(prizes);

                var expertPanel = new List<EventExpert> {
                    new EventExpert { EventId = eventData.EventId, ExpertId = expertId, JoinedAt = DateTime.Now, Status = "Accepted" }
                };

                if (dto.InvitedExpertIds != null)
                {
                    expertPanel.AddRange(dto.InvitedExpertIds.Distinct().Where(id => id != expertId).Select(id => new EventExpert
                    {
                        EventId = eventData.EventId,
                        ExpertId = id,
                        JoinedAt = DateTime.Now,
                        Status = "Pending"
                    }));
                }
                await _eventExpertRepo.AddRangeAsync(expertPanel);

                var escrow = new EscrowSession
                {
                    EventId = eventData.EventId,
                    SenderId = expertId,
                    Amount = totalPrizeMoney,
                    Status = "Held",
                    CreatedAt = DateTime.Now
                };
                await _escrowRepo.AddAsync(escrow);

                decimal balanceBefore = wallet.Balance;
                wallet.Balance -= totalPrizeMoney;
                _walletRepo.Update(wallet);

                await _transactionRepo.AddAsync(new Transaction
                {
                    WalletId = wallet.WalletId,
                    Amount = -totalPrizeMoney,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = wallet.Balance,
                    Type = "Escrow_Hold",
                    ReferenceType = "Event",
                    ReferenceId = eventData.EventId,
                    Status = "Success",
                    CreatedAt = DateTime.Now
                });

                await _unitOfWork.CommitAsync();
                return eventData;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception($"Lỗi tạo Event: {ex.Message}");
            }
        }
        #endregion

        #region Logic Chấm điểm Hội đồng
        public async Task SubmitExpertRatingAsync(ExpertRatingRequest dto)
        {
            int currentExpertId = _currentUserService.GetRequiredUserId();

            var post = await _postRepo.GetByIdAsync(dto.PostId);
            if (post == null || post.EventId == null) throw new Exception("Bài viết không thuộc sự kiện nào.");

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
                await UpdateScoreboardAsync(post);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception($"Lỗi chấm điểm: {ex.Message}");
            }
        }

        private async Task UpdateScoreboardAsync(Post post)
        {
            var ev = await _eventRepo.GetByIdAsync(post.EventId!.Value);
            var ratings = await _ratingRepo.GetRatingsByPostIdAsync(post.PostId);

            double avgExpertScore = ratings.Any() ? ratings.Average(r => r.Score) : 0;
            double communityScore = (post.LikeCount ?? 0) * (double)ev.PointPerLike + (post.ShareCount ?? 0) * (double)ev.PointPerShare;
            double finalScore = (avgExpertScore * (double)ev.ExpertWeight) + (communityScore * (double)ev.UserWeight);

            var sb = await _scoreboardRepo.GetByPostIdAsync(post.PostId);
            if (sb == null)
            {
                await _scoreboardRepo.AddAsync(new Scoreboard
                {
                    PostId = post.PostId,
                    ExpertScore = avgExpertScore,
                    CommunityScore = communityScore,
                    FinalScore = finalScore,
                    FinalLikeCount = post.LikeCount ?? 0,
                    FinalShareCount = post.ShareCount ?? 0,
                    CreatedAt = DateTime.Now,
                    Status = "Judging"
                });
            }
            else
            {
                sb.ExpertScore = avgExpertScore;
                sb.CommunityScore = communityScore;
                sb.FinalScore = finalScore;
                sb.FinalLikeCount = post.LikeCount ?? 0;
                sb.FinalShareCount = post.ShareCount ?? 0;
                _scoreboardRepo.Update(sb);
            }
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

        // Giả lập các hàm Get đơn giản
        public async Task<Event?> GetEventDetailsAsync(int eventId) => await _eventRepo.GetByIdAsync(eventId);
        public async Task<IEnumerable<Event>> GetExpertEventsAsync(int expertId) => await _eventRepo.GetAllByCreatorIdAsync(expertId);
    }
}