using Microsoft.EntityFrameworkCore;
using Repositories.Constants;
using Repositories.Dto.Common;
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

        public async Task<bool> SavePostAsync(int postId, int accountId)
        {
            var post = await _postRepo.GetByIdAsync(postId);
            if (post == null)
                throw new KeyNotFoundException("Post not found.");

            if (post.Status != PostStatus.Published || post.Visibility != PostVisibility.Visible)
                throw new InvalidOperationException("You cannot save this post.");

            var exists = await _postSaveRepo.ExistsAsync(postId, accountId);
            if (exists) return false;

            try
            {
                await _postSaveRepo.AddAsync(new PostSave
                {
                    PostId = postId,
                    AccountId = accountId,
                    CreatedAt = DateTime.UtcNow
                });

                await _uow.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                // trường hợp race condition nhưng DB đã chặn bằng unique index
                return false;
            }
        }

        public async Task<bool> UnsavePostAsync(int postId, int accountId)
        {
            var save = await _postSaveRepo.GetByPostAndUserAsync(postId, accountId);
            if (save == null) return false;

            _postSaveRepo.Delete(save);
            await _uow.SaveChangesAsync();
            return true;
        }

        public Task<PagedResultDto<SavedPostDto>> GetSavedPostsAsync(int accountId, int page, int pageSize)
        {
            return _postSaveRepo.GetSavedPostsAsync(accountId, page, pageSize);
        }
    }
}