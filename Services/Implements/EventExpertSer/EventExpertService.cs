using Repositories.Entities;
using Repositories.Repos.EventExpertRepos;
using Repositories.Repos.Events;
using Repositories.Repos.ExpertRatingRepos;
using Repositories.Repos.PostRepos;
using Repositories.Repos.ScoreboardRepos;
using Repositories.UnitOfWork;
using Services.Implements.Auth;
using Services.Request.ExpertRatingReq;
using Services.Response.EventResp;
using Services.Response.PostResp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services.Implements.EventExpertSer
{
    public class EventExpertService : IEventExpertService
    {
        private readonly IEventExpertRepository _eventExpertRepo;
        private readonly IEventRepository _eventRepo;
        private readonly IPostRepository _postRepo;
        private readonly IScoreboardRepository _scoreboardRepo;
        private readonly IExpertRatingRepository _ratingRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public EventExpertService(
            IEventExpertRepository eventExpertRepo,
            IEventRepository eventRepo,
            IPostRepository postRepo,
            IExpertRatingRepository ratingRepo,
            IScoreboardRepository scoreboardRepo,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _eventExpertRepo = eventExpertRepo;
            _eventRepo = eventRepo;
            _postRepo = postRepo;
            _ratingRepo = ratingRepo;
            _scoreboardRepo = scoreboardRepo;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        #region Expert Commands (Actions)

        /// <summary>
        /// Chủ sự kiện mời danh sách Expert tham gia hội đồng chấm điểm
        /// </summary>
        public async Task<bool> InviteExpertsAsync(int eventId, List<int> expertIds)
        {
            int creatorId = _currentUser.GetRequiredUserId();
            var ev = await _eventRepo.GetByIdAsync(eventId);

            if (ev == null) throw new Exception("Sự kiện không tồn tại.");
            if (ev.CreatorId != creatorId) throw new Exception("Bạn không có quyền mời chuyên gia cho sự kiện này.");
            if (ev.Status != "Pending_Review" && ev.Status != "Inviting")
                throw new Exception("Sự kiện hiện không ở trạng thái có thể mời thêm chuyên gia.");

            var existingExperts = await _eventExpertRepo.GetByEventIdAsync(eventId);
            var existingIds = existingExperts.Select(e => e.ExpertId).ToList();

            var newInvites = expertIds
                .Distinct()
                .Where(id => id != creatorId && !existingIds.Contains(id))
                .Select(id => new EventExpert
                {
                    EventId = eventId,
                    ExpertId = id,
                    JoinedAt = DateTime.Now,
                    Status = "Pending" // Thống nhất dùng Pending cho lời mời mới
                }).ToList();

            if (!newInvites.Any()) return false;

            await _eventExpertRepo.AddRangeAsync(newInvites);
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// Expert phản hồi lời mời: Chấp nhận hoặc Từ chối
        /// </summary>
        public async Task<bool> RespondToInvitationAsync(int eventId, bool accept)
        {
            int currentExpertId = _currentUser.GetRequiredUserId();
            var invite = await _eventExpertRepo.GetByEventAndExpertAsync(eventId, currentExpertId);

            if (invite == null) throw new Exception("Không tìm thấy lời mời.");
            if (invite.Status != "Pending") throw new Exception("Lời mời này đã được xử lý trước đó.");

            invite.Status = accept ? "Accepted" : "Rejected";
            invite.JoinedAt = DateTime.Now;

            _eventExpertRepo.Update(invite);

            // Nếu Expert chấp nhận, kiểm tra xem sự kiện có đủ điều kiện để Bắt đầu (Active) không
            if (accept)
            {
                await CheckAndActivateEventAsync(eventId);
            }

            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        #endregion

        #region Expert Queries (Display)

        /// <summary>
        /// Lấy danh sách LỜI MỜI MỚI (Chưa trả lời) - Hiện ở thông báo/hộp thư
        /// </summary>
        public async Task<IEnumerable<EventListDto>> GetPendingInvitationsAsync()
        {
            int currentExpertId = _currentUser.GetRequiredUserId();
            var relatedEvents = await _eventRepo.GetExpertRelatedEventsAsync(currentExpertId);

            return relatedEvents
                .Where(e => e.CreatorId != currentExpertId &&
                            e.EventExperts.Any(ee => ee.ExpertId == currentExpertId && ee.Status == "Pending") &&
                            e.Status == "Inviting") // Chỉ hiện lời mời khi sự kiện vẫn đang kêu gọi
                .Select(MapToEventListDto);
        }

        /// <summary>
        /// Lấy danh sách SỰ KIỆN ĐANG CHẤM (Đã chấp nhận) - Hiện ở lịch làm việc
        /// </summary>
        public async Task<IEnumerable<EventListDto>> GetEventsInvitedToRateAsync()
        {
            int currentExpertId = _currentUser.GetRequiredUserId();
            var events = await _eventRepo.GetExpertRelatedEventsAsync(currentExpertId);

            return events
                .Where(e => e.CreatorId != currentExpertId &&
                            e.EventExperts.Any(ee => ee.ExpertId == currentExpertId && ee.Status == "Accepted"))
                .Select(MapToEventListDto);
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Tự động chuyển trạng thái sự kiện sang Active nếu đủ số lượng Expert tối thiểu
        /// </summary>
        private async Task CheckAndActivateEventAsync(int eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev == null || ev.Status != "Inviting") return;

            int acceptedCount = ev.EventExperts.Count(ee => ee.Status == "Accepted");

            // Nếu số lượng expert đã đồng ý >= số lượng tối thiểu cấu hình trong Event
            if (acceptedCount >= ev.MinExpertsToStart)
            {
                ev.Status = "Active";
                ev.Note = $"Sự kiện tự động kích hoạt vào {DateTime.Now} vì đã đủ {acceptedCount} chuyên gia.";
                _eventRepo.Update(ev);
            }
        }

        private EventListDto MapToEventListDto(Event e)
        {
            return new EventListDto
            {
                EventId = e.EventId,
                Title = e.Title,
                Description = e.Description,
                Status = e.Status,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                CreatorName = e.Creator?.UserName,
                ParticipantCount = e.Posts?.Count ?? 0,
                ThumbnailUrl = e.Images?.FirstOrDefault()?.ImageUrl,
                TotalPrizePool = e.PrizeEvents?.Sum(p => p.RewardAmount) ?? 0,
                // Trả về danh sách giải thưởng để Expert thấy tầm cỡ sự kiện
                Prizes = e.PrizeEvents?.OrderBy(p => p.Ranked).Select(p => new PrizeBriefDto
                {
                    Ranked = p.Ranked,
                    RewardAmount = p.RewardAmount
                }).ToList() ?? new List<PrizeBriefDto>()
            };
        }

        #endregion

        #region Expert Commands (Actions) - Phần bổ sung

        /// <summary>
        /// Expert thực hiện chấm điểm cho một bài viết
        /// </summary>
        public async Task SubmitExpertRatingAsync(ExpertRatingRequest dto)
        {
            int currentExpertId = _currentUser.GetRequiredUserId();

            // 1. Kiểm tra bài viết và quyền chấm điểm
            var post = await _postRepo.GetByIdAsync(dto.PostId);
            if (post == null || post.EventId == null) throw new Exception("Bài viết không tồn tại hoặc không thuộc sự kiện.");

            var isMember = await _eventExpertRepo.AnyAsync(ee =>
                ee.EventId == post.EventId && ee.ExpertId == currentExpertId && ee.Status == "Accepted");

            if (!isMember) throw new Exception("Bạn không có quyền chấm điểm cho sự kiện này.");

            // 2. Thực hiện lưu điểm (Sử dụng UnitOfWork để đảm bảo tính toàn vẹn khi tính lại Scoreboard)
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

                // 3. Tính toán lại Scoreboard sau khi chấm điểm
                await RecalculateEventScoreboardAsync(post.EventId.Value);

                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception($"Lỗi trong quá trình lưu điểm: {ex.Message}");
            }
        }

        private async Task RecalculateEventScoreboardAsync(int eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev == null) return;

            var allPosts = (await _postRepo.GetPostsByEventIdAsync(eventId)).ToList();
            if (!allPosts.Any()) return;

            // 1. Tìm Max Raw Community Score để chuẩn hóa về thang điểm 10
            double maxRawScore = allPosts.Max(p =>
                (p.LikeCount ?? 0) * ev.PointPerLike + (p.ShareCount ?? 0) * ev.PointPerShare);

            if (maxRawScore <= 0) maxRawScore = 1; // Tránh chia cho 0

            foreach (var p in allPosts)
            {
                // Tính trung bình điểm từ các Expert
                double avgExpertScore = p.ExpertRatings.Any()
                    ? p.ExpertRatings.Average(r => r.Score)
                    : 0;

                // Tính điểm cộng đồng đã chuẩn hóa
                double currentRaw = (p.LikeCount ?? 0) * ev.PointPerLike + (p.ShareCount ?? 0) * ev.PointPerShare;
                double normalizedCommunityScore = (currentRaw / maxRawScore) * 10;

                // Tính điểm cuối cùng theo trọng số (Weight)
                // Ví dụ: Final = (ExpertAvg * 0.6) + (Community * 0.4)
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
                    sb.Status = "Judging";
                    _scoreboardRepo.Update(sb);
                }
            }
        }

        /// <summary>
        /// Lấy danh sách các bài nộp trong một sự kiện để Expert chấm điểm
        /// </summary>
        public async Task<IEnumerable<PostReviewDto>> GetPostsForReviewAsync(int eventId)
        {
            int currentExpertId = _currentUser.GetRequiredUserId();

            // Đảm bảo PostRepo đã lấy đủ các bài Post kèm theo Images và ExpertRatings
            var posts = await _postRepo.GetPostsByEventIdAsync(eventId);

            return posts.Select(p => new PostReviewDto
            {
                PostId = p.PostId,
                Title = p.Title ?? "Chưa có tiêu đề",
                Content = p.Content,
                ImageUrl = p.Images.OrderBy(i => i.ImageId).FirstOrDefault()?.ImageUrl,
                AuthorName = p.Account?.UserName,

                // Trả về điểm Expert đã chấm trước đó để hiển thị trên UI
                CurrentScore = p.ExpertRatings?.FirstOrDefault(r => r.ExpertId == currentExpertId)?.Score,
                MyReason = p.ExpertRatings?.FirstOrDefault(r => r.ExpertId == currentExpertId)?.Reason,
                IsGraded = p.ExpertRatings?.Any(r => r.ExpertId == currentExpertId) ?? false,

                LikeCount = p.LikeCount ?? 0,
                ShareCount = p.ShareCount ?? 0,
                SubmittedAt = p.CreatedAt ?? DateTime.Now
            });
        }

        #endregion
    }
}