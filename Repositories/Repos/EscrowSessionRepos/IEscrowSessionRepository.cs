using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.EscrowSessionRepos
{
    public interface IEscrowSessionRepository
    {
        Task AddAsync(EscrowSession session);
        Task<EscrowSession?> GetByIdAsync(int sessionId);
        Task<EscrowSession?> GetByEventIdAsync(int eventId);
        void Update(EscrowSession session);
    }
}
