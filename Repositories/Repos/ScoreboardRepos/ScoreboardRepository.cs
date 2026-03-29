using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.ScoreboardRepos
{
    public class ScoreboardRepository : IScoreboardRepository
    {
        private readonly FashionDbContext _context;
        public ScoreboardRepository(FashionDbContext context) => _context = context;

        public async Task AddAsync(Scoreboard scoreboard) =>
            await _context.Scoreboards.AddAsync(scoreboard);

        public void Update(Scoreboard scoreboard) =>
            _context.Scoreboards.Update(scoreboard);

        public async Task<Scoreboard?> GetByPostIdAsync(int postId) =>
            await _context.Scoreboards
                .FirstOrDefaultAsync(s => s.PostId == postId);

        public async Task<IEnumerable<Scoreboard>> GetLeaderboardByEventIdAsync(int eventId)
        {
            return await _context.Scoreboards
                .Include(s => s.Post)
                .Where(s => s.Post.EventId == eventId)
                .OrderByDescending(s => s.FinalScore)
                .ToListAsync();
        }
    }
}
