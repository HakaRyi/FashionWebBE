using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.ScoreboardRepos
{
    public interface IScoreboardRepository
    {
        Task AddAsync(Scoreboard scoreboard);
        void Update(Scoreboard scoreboard);
        Task<Scoreboard?> GetByPostIdAsync(int postId);
        Task<IEnumerable<Scoreboard>> GetLeaderboardByEventIdAsync(int eventId);
    }
}
