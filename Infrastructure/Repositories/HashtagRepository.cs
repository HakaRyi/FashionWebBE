using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class HashtagRepository : IHashtagRepository
    {
        private readonly FashionDbContext _db;

        public HashtagRepository(FashionDbContext db)
        {
            _db = db;
        }

        public async Task<Hashtag?> GetByNameAsync(string name)
        {
            return await _db.Hashtags
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Name.ToLower() == name.ToLower());
        }

        public async Task<Hashtag?> GetByIdAsync(int hashtagId)
        {
            return await _db.Hashtags
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.HashtagId == hashtagId);
        }

        public async Task<List<Hashtag>> GetByNamesAsync(
            List<string> names)
        {
            var normalized = names
                .Select(x => x.ToLower())
                .ToList();

            return await _db.Hashtags
                .Where(x =>
                    normalized.Contains(x.Name.ToLower()))
                .ToListAsync();
        }

        public async Task<List<Hashtag>> GetTrendingSourceAsync(
            int days = 7)
        {
            var threshold = DateTime.UtcNow.AddDays(-days);

            return await _db.Hashtags
                .AsNoTracking()
                .Include(x => x.PostHashtags)
                    .ThenInclude(x => x.Post)
                .Where(x =>
                    x.PostHashtags.Any(ph =>
                        ph.Post.CreatedAt.HasValue &&
                        ph.Post.CreatedAt.Value >= threshold &&
                        ph.Post.Status == PostStatus.Published &&
                        ph.Post.Visibility == PostVisibility.Visible))
                .ToListAsync();
        }

        public async Task<List<Hashtag>> GetTopUsedAsync(
            int limit)
        {
            return await _db.Hashtags
                .AsNoTracking()
                .OrderByDescending(x => x.UsageCount)
                .Take(limit)
                .ToListAsync();
        }

        public async Task AddAsync(Hashtag hashtag)
        {
            await _db.Hashtags.AddAsync(hashtag);
        }

        public async Task AddRangeAsync(
            List<Hashtag> hashtags)
        {
            await _db.Hashtags.AddRangeAsync(hashtags);
        }

        public void Update(Hashtag hashtag)
        {
            _db.Hashtags.Update(hashtag);
        }

        public async Task<List<Hashtag>> SearchTagsAsync(string query, int limit)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<Hashtag>();

            var normalized = query.Trim().ToLower();

            return await _db.Hashtags
                .AsNoTracking()
                .Where(x => x.Name.ToLower().Contains(normalized))
                .OrderByDescending(x => x.UsageCount) 
                .Take(limit)
                .ToListAsync();
        }
    }
}