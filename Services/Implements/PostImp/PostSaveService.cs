using Repositories.Constants;
using Repositories.Dto.Social.SavedPost;
using Repositories.Entities;
using Repositories.Repos.PostRepos;
using Repositories.Repos.PostSaveRepos;
using Repositories.UnitOfWork;

namespace Services.Implements.PostImp
{
    public class PostSaveService : IPostSaveService
    {
        private readonly IPostRepository _postRepo;
        private readonly IPostSaveRepository _postSaveRepo;
        private readonly IUnitOfWork _uow;

        public PostSaveService(
            IPostRepository postRepo,
            IPostSaveRepository postSaveRepo,
            IUnitOfWork uow)
        {
            _postRepo = postRepo;
            _postSaveRepo = postSaveRepo;
            _uow = uow;
        }

        public async Task SavePostAsync(int postId, int accountId)
        {
            var post = await _postRepo.GetByIdAsync(postId)
                ?? throw new Exception("Post not found");

            if (post.Status != PostStatus.Published ||
                post.Visibility != PostVisibility.Visible)
            {
                throw new UnauthorizedAccessException("You cannot save this post.");
            }

            var exists = await _postSaveRepo.ExistsAsync(postId, accountId);
            if (exists) return;

            var save = new PostSave
            {
                PostId = postId,
                AccountId = accountId,
                CreatedAt = DateTime.UtcNow
            };

            await _postSaveRepo.AddAsync(save);
            await _uow.SaveChangesAsync();
        }

        public async Task UnsavePostAsync(int postId, int accountId)
        {
            var save = await _postSaveRepo.GetByPostAndUserAsync(postId, accountId);
            if (save == null) return;

            _postSaveRepo.Delete(save);
            await _uow.SaveChangesAsync();
        }

        public Task<List<SavedPostDto>> GetSavedPostsAsync(int accountId, int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            return _postSaveRepo.GetSavedPostsAsync(accountId, page, pageSize);
        }
    }
}