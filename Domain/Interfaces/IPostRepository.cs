using Domain.Contracts.Common;
using Domain.Contracts.Social.Post;
using Domain.Dto.Admin;
using Domain.Entities;

namespace Domain.Interfaces

{
    public interface IPostRepository
    {
        Task<List<Post>> GetAllPostAsync();
        Task<Post?> GetByIdAsync(int postId);
        Task<Post?> GetByIdFullRangeAsync(int postId);
        Task<List<PostFeedDto>> GetFeedWithSocialAsync(int viewerId, DateTime? cursor, int pageSize);
        Task<PostDetailDto?> GetPostDetailAsync(int postId, int viewerId);
        Task<PagedResultDto<MyPostDto>> GetMyPostsPagedAsync(int ownerId, int page, int pageSize);
        Task<PagedResultDto<PostFeedDto>> GetUserPublicPostsPagedAsync(int ownerId, int? viewerId, int page, int pageSize);
        Task<List<PostFeedDto>> GetTrendingPostsAsync(int limit, int viewerId);
        Task AddAsync(Post post);
        void Update(Post post);
        void Delete(Post post);
        Task<List<Post>> GetAllPendingAdminPostAsync();
        Task<List<Post>> GetAllPublishedAsync();
        Task<List<Post>> GetAllByUserAsync(int userId);
        Task<IEnumerable<Post>> GetPostsByEventIdAsync(int eventId);
        Task<IEnumerable<Post>> GetPostsForReview(int eventId, int? accountId);
        Task<Post?> GetPostForShareAsync(int postId);
        Task<double> GetMaxRawCommunityScoreAsync(int eventId, double pointPerLike, double pointPerShare);
        Task<List<Post>> GetGradedPostsByEventIdAsync(int eventId);
        Task<int> CountAccountPostsAsync(int accountId);

        Task<(List<Post> Posts, List<Account> Users)> SearchRawDataAsync(string keyword, int limit);
        Task<List<int>> GetLikedPostIdsAsync(int viewerId, List<int> postIds);
    }
}