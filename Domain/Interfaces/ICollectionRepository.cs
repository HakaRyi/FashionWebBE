using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface ICollectionRepository
    {
        Task CreateAsync(Collection collection);
        Task UpdateAsync(Collection collection);
        Task<Collection?> GetByIdAsync(int id, int accountId);
        Task<List<Collection>> GetByAccountIdAsync(int accountId);
        Task DeleteAsync(Collection collection);
    }
}
