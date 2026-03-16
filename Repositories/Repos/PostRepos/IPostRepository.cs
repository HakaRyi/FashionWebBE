using Repositories.Dto.Common;
using Repositories.Dto.Social.Post;
using Repositories.Entities;

namespace Repositories.Repos.PostRepos
{
    public interface IPostRepository
    {
        Task<Post?> GetByIdAsync(int postId);

        Task<List<PostFeedDto>> GetFeedWithSocialAsync(
            int viewerId,
            DateTime? cursor,
            int pageSize);

        Task<PostDetailDto?> GetPostDetailAsync(
            int postId,
            int viewerId);

        Task<PagedResultDto<MyPostDto>> GetMyPostsPagedAsync(
            int ownerId,
            int page,
            int pageSize);

        Task<PagedResultDto<PostFeedDto>> GetUserPublicPostsPagedAsync(
            int ownerId,
            int? viewerId,
            int page,
            int pageSize);

        Task<List<PostFeedDto>> GetTrendingPostsAsync(
            int limit,
            int viewerId);

        Task AddAsync(Post post);

        void Update(Post post);

        void Delete(Post post);
    }
}