using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Repositories
{
    public class ExpertRatingRepository : IExpertRatingRepository
    {
        private readonly FashionDbContext _context;
        public ExpertRatingRepository(FashionDbContext context) => _context = context;

        public async Task AddAsync(ExpertRating rating) =>
            await _context.ExpertRatings.AddAsync(rating);

        public void Update(ExpertRating rating) =>
            _context.ExpertRatings.Update(rating);

        public async Task<ExpertRating?> GetByPostAndExpertAsync(int postId, int expertId) =>
            await _context.ExpertRatings
                .Include(r => r.CriterionRatings)
                .FirstOrDefaultAsync(r => r.PostId == postId && r.ExpertId == expertId);

        public async Task<IEnumerable<ExpertRating>> GetRatingsByPostIdAsync(int postId) =>
            await _context.ExpertRatings
                .Where(r => r.PostId == postId)
                .ToListAsync();
    }
}
