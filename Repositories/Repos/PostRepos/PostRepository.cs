using Microsoft.EntityFrameworkCore;
using Repositories.Constants;
using Repositories.Data;
using Repositories.Entities;
using System.Threading.Tasks;

namespace Repositories.Repos.PostRepos
{
    public class PostRepository : IPostRepository
    {
        private readonly FashionDbContext _context;

        public PostRepository(FashionDbContext context)
        {
            _context = context;
        }

        public async Task<Post?> GetByIdAsync(int postId)
        {
            return await _context.Posts
                .Include(p => p.Images)
                .Include(p => p.Account)
                    .ThenInclude(a => a.Avatars)
                .Include(p => p.Event)
                .FirstOrDefaultAsync(p => p.PostId == postId);
        }

        public async Task<List<Post>> GetAllPublishedAsync()
        {
            return await _context.Posts
                .Include(p => p.Images)
                .Include(p => p.Account)
                    .ThenInclude(a => a.Avatars)
                .Include(p => p.Event)
                .Where(p => p.Status == PostStatus.Published)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Post>> GetAllByUserAsync(int userId)
        {
            return await _context.Posts
                .Include(p => p.Images)
                .Include(p => p.Account)
                    .ThenInclude(a => a.Avatars)
                .Include(p => p.Event)
                .Where(p => p.AccountId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Post>> GetFeedByCursorAsync(DateTime? cursor, int pageSize)
        {
            var query = _context.Posts
                .Include(p => p.Images)
                .Include(p => p.Account)
                    .ThenInclude(a => a.Avatars)
                .Include(p => p.Event)
                .Where(p => p.Status == PostStatus.Published);

            if (cursor.HasValue)
            {
                query = query.Where(p => p.CreatedAt < cursor.Value);
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task AddAsync(Post post)
        {
            await _context.Posts.AddAsync(post);
        }

        public async Task  Update(Post post)
        {
            _context.Posts.Update(post);
            await _context.SaveChangesAsync();
        }

        public void Delete(Post post)
        {
            _context.Posts.Remove(post);
        }
    }
}