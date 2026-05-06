using Application.Interfaces;
using Application.Request.ExpertRatingReq;
using Application.Response.EventResp;
using Domain.Entities;
using Domain.Interfaces;
using Mapster;
using static Application.Response.EventResp.EventEndedResponse;

namespace Application.Services.EventServices
{
    public class ExpertRatingService : IExpertRatingService
    {
        private readonly IEventRepository _eventRepo;
        private readonly IEventExpertRepository _eventExpertRepo;
        private readonly IExpertRatingRepository _ratingRepo;
        private readonly IEventCriterionRepository _criterionRepo;
        private readonly IPostRepository _postRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public ExpertRatingService(
            IEventRepository eventRepo,
            IEventExpertRepository eventExpertRepo,
            IExpertRatingRepository ratingRepo,
            IEventCriterionRepository criterionRepo,
            IPostRepository postRepo,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService)
        {
            _eventRepo = eventRepo;
            _eventExpertRepo = eventExpertRepo;
            _ratingRepo = ratingRepo;
            _criterionRepo = criterionRepo;
            _postRepo = postRepo;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<IEnumerable<EventCriterionResponse>> GetEventCriteriaForRating(int eventId)
        {
            var criteriaEntities = await _criterionRepo.GetCriteriaByEventIdAsync(eventId);

            return criteriaEntities.Adapt<IEnumerable<EventCriterionResponse>>();
        }

        public async Task SubmitExpertRatingAsync(ExpertRatingRequest dto)
        {
            int currentExpertId = _currentUserService.GetRequiredUserId();

            var post = await _postRepo.GetByIdAsync(dto.PostId);
            if (post == null || post.EventId == null)
                throw new Exception("The article does not exist or is not related to any event.");

            var ev = await _eventRepo.GetByIdAsync(post.EventId.Value);
            if (ev == null || (ev.Status != "Active" && ev.Status != "Judging"))
                throw new Exception("The event was not within the allowed scoring time.");

            var isMember = await _eventExpertRepo.AnyAsync(ee =>
                ee.EventId == post.EventId && ee.ExpertId == currentExpertId && ee.Status == "Accepted");
            if (!isMember)
                throw new Exception("You are not allowed to rate this event.");

            var eventCriteria = await _criterionRepo.GetCriteriaByEventIdAsync(post.EventId.Value);
            if (!eventCriteria.Any())
                throw new Exception("This event does not yet have established scoring criteria.");

            double calculatedTotalScore = 0;
            var newCriterionRatings = new List<ExpertCriterionRating>();

            foreach (var criteria in eventCriteria)
            {
                var inputRating = dto.CriterionRatings.FirstOrDefault(c => c.EventCriterionId == criteria.EventCriterionId);
                if (inputRating == null)
                    throw new Exception($"Please provide a full score for each criterion: {criteria.Name}");

                calculatedTotalScore += (inputRating.Score * criteria.WeightPercentage) / 100.0;

                newCriterionRatings.Add(new ExpertCriterionRating
                {
                    EventCriterionId = criteria.EventCriterionId,
                    Score = inputRating.Score
                });
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var existingRating = await _ratingRepo.GetByPostAndExpertAsync(dto.PostId, currentExpertId);

                if (existingRating != null)
                {
                    existingRating.Score = calculatedTotalScore;
                    existingRating.Reason = dto.Reason;
                    existingRating.UpdatedAt = DateTime.UtcNow;

                    foreach (var detail in existingRating.CriterionRatings)
                    {
                        var newDetail = newCriterionRatings.First(x => x.EventCriterionId == detail.EventCriterionId);
                        detail.Score = newDetail.Score;
                    }

                    _ratingRepo.Update(existingRating);
                }
                else
                {
                    var newRating = new ExpertRating
                    {
                        PostId = dto.PostId,
                        ExpertId = currentExpertId,
                        Score = calculatedTotalScore,
                        Reason = dto.Reason,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        CriterionRatings = newCriterionRatings
                    };

                    await _ratingRepo.AddAsync(newRating);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception($"Error during scoring: System error. Details: {ex.Message}");
            }
        }

        public async Task<PostRatingDetailResponse> GetPostRatingDetailsAsync(int postId)
        {
            var post = await _postRepo.GetByIdFullRangeAsync(postId);
            if (post == null) throw new Exception("No article found.");

            var expertRatings = await _ratingRepo.GetDetailedRatingsByPostIdAsync(postId);

            int currentExpertId = _currentUserService.GetRequiredUserId();
            bool isExpert = post.Event?.EventExperts?.Any(ee => ee.ExpertId == currentExpertId) ?? false;

            var response = new PostRatingDetailResponse
            {
                PostId = post.PostId,
                Title = post.Title ?? "Untitled",
                FinalLikeCount = post.Scoreboard?.FinalLikeCount ?? 0,
                FinalShareCount = post.Scoreboard?.FinalShareCount ?? 0,
                FinalScore = post.Scoreboard?.FinalScore ?? 0,
                CommunityScore = post.Scoreboard?.CommunityScore ?? 0,
                ExpertTotalScore = post.Scoreboard?.ExpertScore ?? 0,
                IsExpert = isExpert,
                PointPerLike = post.Event?.PointPerLike,
                PointPerShare = post.Event?.PointPerShare,
                ExpertReviews = expertRatings.Select(rating => new ExpertReviewDetail
                {
                    ExpertId = rating.ExpertId,
                    ExpertName = rating.Expert?.UserName ?? "Unknown Expert",
                    TotalScoreGiven = rating.Score,
                    Reason = rating.Reason,
                    RatedAt = rating.UpdatedAt,
                    CriteriaScores = rating.CriterionRatings.Select(cr => new CriterionScoreDetail
                    {
                        CriterionName = cr.EventCriterion?.Name ?? "Unknown Criterion",
                        Score = cr.Score,
                        WeightPercentage = cr.EventCriterion?.WeightPercentage ?? 0
                    }).ToList()
                }).ToList()
            };

            return response;
        }
    }
}