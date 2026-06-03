using Application.Interfaces;
using Application.Request.EscrowStatusHistoryReq;
using Application.Response.EscrowStatusHistoryResp;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services
{
    public class EscrowStatusHistoryService : IEscrowStatusHistoryService
    {
        private readonly IEscrowStatusHistoryRepository _escrowStatusHistoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public EscrowStatusHistoryService(IEscrowStatusHistoryRepository escrowStatusHistoryRepository, IUnitOfWork unitOfWork)
        {
            _escrowStatusHistoryRepository = escrowStatusHistoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<EscrowStatusHistoryResponse?> GetByIdAsync(int id)
        {
            var history = await _escrowStatusHistoryRepository.GetByIdAsync(id);
            if (history == null) return null;

            return new EscrowStatusHistoryResponse
            {
                EscrowStatusHistoryId = history.EscrowStatusHistoryId,
                EscrowSessionId = history.EscrowSessionId,
                FromStatus = history.FromStatus,
                ToStatus = history.ToStatus,
                AmountBefore = history.AmountBefore,
                AmountAfter = history.AmountAfter,
                Reason = history.Reason,
                ChangedById = history.ChangedById,
                ChangedByName = history.ChangedBy?.UserName ?? "System",
                ChangedAt = history.ChangedAt
            };
        }

        public async Task<IEnumerable<EscrowStatusHistoryResponse>> GetByEscrowSessionIdAsync(int escrowSessionId)
        {
            var histories = await _escrowStatusHistoryRepository.GetByEscrowSessionIdAsync(escrowSessionId);

            return histories.Select(h => new EscrowStatusHistoryResponse
            {
                EscrowStatusHistoryId = h.EscrowStatusHistoryId,
                EscrowSessionId = h.EscrowSessionId,
                FromStatus = h.FromStatus,
                ToStatus = h.ToStatus,
                AmountBefore = h.AmountBefore,
                AmountAfter = h.AmountAfter,
                Reason = h.Reason,
                ChangedById = h.ChangedById,
                ChangedByName = h.ChangedBy?.UserName ?? "System",
                ChangedAt = h.ChangedAt
            }).ToList();
        }

        public async Task<EscrowStatusHistoryResponse> CreateHistoryAsync(CreateEscrowStatusHistoryRequest request)
        {
            var historyEntity = new EscrowStatusHistory
            {
                EscrowSessionId = request.EscrowSessionId,
                FromStatus = request.FromStatus,
                ToStatus = request.ToStatus,
                AmountBefore = request.AmountBefore,
                AmountAfter = request.AmountAfter,
                Reason = request.Reason,
                ChangedById = request.ChangedById,
                ChangedAt = DateTime.UtcNow
            };

            await _escrowStatusHistoryRepository.AddAsync(historyEntity);

            await _unitOfWork.SaveChangesAsync();

            return new EscrowStatusHistoryResponse
            {
                EscrowStatusHistoryId = historyEntity.EscrowStatusHistoryId,
                EscrowSessionId = historyEntity.EscrowSessionId,
                FromStatus = historyEntity.FromStatus,
                ToStatus = historyEntity.ToStatus,
                AmountBefore = historyEntity.AmountBefore,
                AmountAfter = historyEntity.AmountAfter,
                Reason = historyEntity.Reason,
                ChangedById = historyEntity.ChangedById,
                ChangedAt = historyEntity.ChangedAt
            };
        }
    }
}
