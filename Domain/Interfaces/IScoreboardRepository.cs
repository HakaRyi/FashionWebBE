using Domain.Entities;

namespace Domain.Interfaces

{
    public interface IScoreboardRepository
    {
        Task AddAsync(Scoreboard scoreboard);
        void Update(Scoreboard scoreboard);
        Task<Scoreboard?> GetByPostIdAsync(int postId);
        Task<IEnumerable<Scoreboard>> GetLeaderboardByEventIdAsync(int eventId);
    }
}
