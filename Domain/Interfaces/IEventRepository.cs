using Domain.Entities;

namespace Domain.Interfaces

{
    public interface IEventRepository
    {
        //generall
        Task<Event?> GetByIdAsync(int id);
        Task<IEnumerable<Event>> GetAllAsync(string[]? statuses = null);
        Task AddAsync(Event @event);
        void Update(Event @event);
        void Delete(Event @event);

        //admin

        //user
        Task<IEnumerable<Event>> GetPublicEventsAsync();

        //expert
        Task<IEnumerable<Event>> GetExpertRelatedEventsAsync(int expertId);
        Task<IEnumerable<Event>> GetAnalyticsDataAsync(int creatorId, DateTime startDate);
        Task<IEnumerable<Event>> GetAllByCreatorIdAsync(int creatorId);

        Task<List<Scoreboard>> GetLeaderboardAsync(int eventId);
        Task<Scoreboard?> GetUserScoreAsync(int eventId, int accountId);
        Task<List<ExpertRating>> GetExpertRatingsForPostAsync(int postId);
        Task<List<Reaction>> GetPostVotersAsync(int postId);

    }
}
