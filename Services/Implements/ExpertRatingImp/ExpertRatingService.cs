using Repositories.Entities;
using Repositories.Repos.EventExpertRepos;
using Repositories.Repos.Events;
using Repositories.Repos.ExpertRatingRepos;
using Repositories.Repos.PostRepos;
using Repositories.UnitOfWork;
using Services.Implements.Auth;
using Services.Request.ExpertRatingReq;


namespace Services.Implements.ExpertRatingImp
{
    public class ExpertRatingService : IExpertRatingService
    {

        private readonly IEventRepository _eventRepo;
        private readonly IEventExpertRepository _eventExpertRepo;
        private readonly IExpertRatingRepository _ratingRepo;
        private readonly IPostRepository _postRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public ExpertRatingService(
            IEventRepository eventRepo,
            IEventExpertRepository eventExpertRepo,
            IExpertRatingRepository ratingRepo,
            IPostRepository postRepo,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService)
        {
            _eventRepo = eventRepo;
            _eventExpertRepo = eventExpertRepo;
            _ratingRepo = ratingRepo;
            _postRepo = postRepo;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task SubmitExpertRatingAsync(ExpertRatingRequest dto)
        {
            int currentExpertId = _currentUserService.GetRequiredUserId();

            var post = await _postRepo.GetByIdAsync(dto.PostId);
            if (post == null || post.EventId == null) throw new Exception("Bài viết không tồn tại hoặc không thuộc sự kiện nào.");

            var ev = await _eventRepo.GetByIdAsync(post.EventId.Value);

            if (ev == null || (ev.Status != "Active" && ev.Status != "Judging"))
                throw new Exception("Sự kiện không trong thời gian cho phép chấm điểm.");

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
                    existingRating.UpdatedAt = DateTime.UtcNow;
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

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception($"Lỗi trong quá trình chấm điểm: Lỗi hệ thống.");
            }
        }
    }
}
