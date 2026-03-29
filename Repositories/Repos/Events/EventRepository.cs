using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.Events
{
    public class EventRepository : IEventRepository
    {
        private readonly FashionDbContext _db;
        public EventRepository(FashionDbContext db) => _db = db;

        public async Task<Event?> GetByIdAsync(int id)
            => await _db.Events
                .Include(e => e.Creator)
                .Include(e=>e.Images)
                .Include(e => e.Posts)
                .Include(e => e.PrizeEvents)
                .Include(e => e.EventExperts)
                    .ThenInclude(ee => ee.Expert).ThenInclude(ex=>ex.Avatars)
                .FirstOrDefaultAsync(e => e.EventId == id);

        public async Task<IEnumerable<Event>> GetAllByCreatorIdAsync(int creatorId)
            => await _db.Events
                .Include(e => e.Creator)
                    .ThenInclude(ex => ex.Avatars)
                .Include(e => e.Posts)
                .Include(e => e.Images)
                .Include(e => e.PrizeEvents)
                .Where(e => e.CreatorId == creatorId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

        public async Task<IEnumerable<Event>> GetAnalyticsDataAsync(int creatorId, DateTime startDate)
        {
            return await _db.Events
                .Where(e => e.CreatorId == creatorId && e.CreatedAt >= startDate)
                .Include(e => e.Posts)
                    .ThenInclude(p => p.Scoreboard)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(Event @event)
        {
            await _db.Events.AddAsync(@event);
        }

        public void Update(Event @event) => _db.Events.Update(@event);

        public void Delete(Event @event) => _db.Events.Remove(@event);

        public async Task<IEnumerable<Event>> GetAllAsync(string[]? statuses = null)
        {
            var query = _db.Events
                .Include(e => e.Creator)
                    .ThenInclude(ex => ex.Avatars)
                .Include(e => e.Images)
                .Include(e => e.Posts)
                .Include(e => e.PrizeEvents)
                .AsQueryable();

            if (statuses != null && statuses.Length > 0)
            {
                query = query.Where(e => statuses.Contains(e.Status));
            }

            return await query
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Event>> GetPublicEventsAsync()
        {
            var publicStatuses = new[] { "Inviting", "Active", "Completed" };
            return await _db.Events
                .Include(e => e.Creator)
                .Include(e => e.Posts)
                .Where(e => publicStatuses.Contains(e.Status))
                .OrderByDescending(e => e.StartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Event>> GetExpertRelatedEventsAsync(int expertId)
        {
            return await _db.Events
                .Include(e => e.Creator)
                .Include(e => e.Posts)
                .Include(e => e.EventExperts)
                .Where(e => e.CreatorId == expertId || e.EventExperts.Any(ee => ee.ExpertId == expertId))
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Scoreboard>> GetLeaderboardAsync(int eventId)
        {
            return await _db.Scoreboards
                .Include(s => s.Post).ThenInclude(p => p.Account).ThenInclude(a => a.Avatars)
                .Where(s => s.Post.EventId == eventId)
                .OrderByDescending(s => s.FinalScore)
                .ToListAsync();
        }

        public async Task<Scoreboard?> GetUserScoreAsync(int eventId, int accountId)
        {
            return await _db.Scoreboards
                .Include(s => s.Post)
                .FirstOrDefaultAsync(s => s.Post.EventId == eventId && s.Post.AccountId == accountId);
        }

        public async Task<List<ExpertRating>> GetExpertRatingsForPostAsync(int postId)
        {
            return await _db.ExpertRatings
                .Include(r => r.Expert).ThenInclude(e => e.Avatars)
                .Where(r => r.PostId == postId)
                .ToListAsync();
        }

        public async Task<List<Reaction>> GetPostVotersAsync(int postId)
        {
            return await _db.Reactions
                .Include(r => r.Account).ThenInclude(a => a.Avatars)
                .Where(r => r.PostId == postId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}
