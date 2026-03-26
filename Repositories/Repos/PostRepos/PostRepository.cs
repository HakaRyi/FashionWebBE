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
                .Include(p => p.Event)
                .FirstOrDefaultAsync(p => p.PostId == postId);
        }

        public async Task<List<Post>> GetAllPublishedAsync()
        {
            return await _context.Posts
                .Include(p => p.Images)
                .Include(p => p.Account)
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
                .Include(p => p.Event)
                .Where(p => p.AccountId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetPostsByEventIdAsync(int eventId)
        {
            return await _context.Posts
                .Include(p => p.Account)
                .Include(p => p.Images)
                .Include(p => p.ExpertRatings)
                .Where(p => p.EventId == eventId && p.Status == "Active")
                .ToListAsync();
        }

        public async Task<double> GetMaxRawCommunityScoreAsync(int eventId, double pointPerLike, double pointPerShare)
        {
            var maxRawScore = await _context.Posts
                .Where(p => p.EventId == eventId && p.Status != "Deleted")
                .MaxAsync(p => (double?)((p.LikeCount ?? 0) * pointPerLike + (p.ShareCount ?? 0) * pointPerShare)) ?? 0;

            return maxRawScore;
        }

        public async Task<List<Post>> GetGradedPostsByEventIdAsync(int eventId)
        {
            return await _context.Posts
                .Include(p => p.Scoreboard)
                .Where(p => p.EventId == eventId && p.Scoreboard != null)
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