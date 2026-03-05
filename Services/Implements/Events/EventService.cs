using Repositories.Data;
using Repositories.Entities;
using Repositories.Repos.Events;
using Repositories.Repos.ExpertProfileRepos;
using Services.Response.EventResp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.Events
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepo;
        private readonly IExpertProfileRepository _expertRepo;
        private readonly FashionDbContext _db;

        public EventService(IEventRepository eventRepo, IExpertProfileRepository expertRepo, FashionDbContext db)
        {
            _eventRepo = eventRepo;
            _expertRepo = expertRepo;
            _db = db;
        }

        public async Task<bool> CreateEventAsync(int accountId, CreateEventDto dto)
        {
            //double totalBudget = dto.Prizes.Sum(p => p.Amount);

            //var account = await _db.Accounts.FindAsync(accountId);
            //if (account == null || account.Coins < totalBudget) return false;

            //account.Coins -= totalBudget;

            var newEvent = new Event
            {
                CreatorId = accountId,
                Title = dto.Title,
                Description = dto.Description,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            await _eventRepo.AddAsync(newEvent);
            return await _eventRepo.SaveChangesAsync();
        }

        public async Task<bool> DepositCoinsAsync(DepositDto dto)
        {
            var account = await _db.Accounts.FindAsync(dto.AccountId);
            if (account == null) return false;

            //account.Coins += dto.Amount;

            return await _db.SaveChangesAsync() > 0;
        }

        public async Task CalculateFinalScoreAsync(int postId, double expertGrade, double communityGrade, double weight)
        {
            var post = await _db.Posts.FindAsync(postId);
            if (post != null)
            {

                double finalScore = (expertGrade * (weight / 100)) + (communityGrade * (1 - (weight / 100)));
                post.Score = finalScore;
                _db.Posts.Update(post);
                await _db.SaveChangesAsync();
            }
        }
    }
}
