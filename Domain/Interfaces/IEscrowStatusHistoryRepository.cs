using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IEscrowStatusHistoryRepository
    {
        Task<EscrowStatusHistory?> GetByIdAsync(int id);
        Task<IEnumerable<EscrowStatusHistory>> GetByEscrowSessionIdAsync(int escrowSessionId);
        Task AddAsync(EscrowStatusHistory escrowStatusHistory);
        void Update(EscrowStatusHistory escrowStatusHistory);
        void Delete(EscrowStatusHistory escrowStatusHistory);
    }
}
