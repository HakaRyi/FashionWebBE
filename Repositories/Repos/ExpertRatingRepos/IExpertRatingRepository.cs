using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.ExpertRatingRepos
{
    public interface IExpertRatingRepository
    {
        Task AddAsync(ExpertRating rating);
        void Update(ExpertRating rating);
        Task<ExpertRating?> GetByPostAndExpertAsync(int postId, int expertId);
        Task<IEnumerable<ExpertRating>> GetRatingsByPostIdAsync(int postId);
    }
}
