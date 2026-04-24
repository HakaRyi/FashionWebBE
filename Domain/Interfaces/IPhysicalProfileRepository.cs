using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IPhysicalProfileRepository
    {
        Task AddAsync(PhysicalProfile profile);
        Task<PhysicalProfile?> GetCurrentByAccountIdAsync(int accountId);
        Task<List<PhysicalProfile>> GetHistoryByAccountIdAsync(int accountId);
    }
}
