using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.ExpertRatingRepos
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
                .FirstOrDefaultAsync(r => r.PostId == postId && r.ExpertId == expertId);

        public async Task<IEnumerable<ExpertRating>> GetRatingsByPostIdAsync(int postId) =>
            await _context.ExpertRatings
                .Where(r => r.PostId == postId)
                .ToListAsync();
    }
}
