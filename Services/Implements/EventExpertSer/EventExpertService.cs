using Repositories.Entities;
using Repositories.Repos.EventExpertRepos;
using Repositories.Repos.Events;
using Repositories.UnitOfWork;
using Services.Implements.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.EventExpertSer
{
    public class EventExpertService : IEventExpertService
    {
        private readonly IEventExpertRepository _eventExpertRepo;
        private readonly IEventRepository _eventRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public EventExpertService(
            IEventExpertRepository eventExpertRepo,
            IEventRepository eventRepo,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _eventExpertRepo = eventExpertRepo;
            _eventRepo = eventRepo;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<bool> InviteExpertsAsync(int eventId, List<int> expertIds)
        {
            int creatorId = _currentUser.GetRequiredUserId();
            var ev = await _eventRepo.GetByIdAsync(eventId);

            if (ev == null) throw new Exception("Sự kiện không tồn tại.");
            if (ev.CreatorId != creatorId) throw new Exception("Bạn không phải chủ sự kiện.");

            // Lọc bỏ những người đã có trong danh sách (dù là Pending hay Accepted)
            var existingExperts = await _eventExpertRepo.GetByEventIdAsync(eventId);
            var existingIds = existingExperts.Select(e => e.ExpertId).ToList();

            var newInvites = expertIds
                .Distinct()
                .Where(id => id != creatorId && !existingIds.Contains(id))
                .Select(id => new EventExpert
                {
                    EventId = eventId,
                    ExpertId = id,
                    JoinedAt = DateTime.Now,
                    Status = "Pending"
                }).ToList();

            if (!newInvites.Any()) return false;

            await _eventExpertRepo.AddRangeAsync(newInvites);
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<bool> RespondToInvitationAsync(int eventId, bool accept)
        {
            int currentExpertId = _currentUser.GetRequiredUserId();
            var invite = await _eventExpertRepo.GetByEventAndExpertAsync(eventId, currentExpertId);

            if (invite == null) throw new Exception("Không tìm thấy lời mời.");
            if (invite.Status != "Pending") throw new Exception("Lời mời này đã được xử lý.");

            invite.Status = accept ? "Accepted" : "Rejected";
            invite.JoinedAt = DateTime.Now;

            _eventExpertRepo.Update(invite);
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<Event>> GetMyPendingInvitationsAsync()
        {
            int currentExpertId = _currentUser.GetRequiredUserId();
            var eventIds = await _eventExpertRepo.GetEventIdsByStatusAsync(currentExpertId, "Pending");

            var events = new List<Event>();
            foreach (var id in eventIds)
            {
                var ev = await _eventRepo.GetByIdAsync(id);
                if (ev != null) events.Add(ev);
            }
            return events;
        }
    }
}
