using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Domain.Interfaces

{
    public interface IWardrobeRepository
    {
        Task<int> CreateWardrobe(Wardrobe wardrobe);
        Task<List<Wardrobe>> GetAll();
        Task<Wardrobe?> GetByIdAsync(int wardrobeId);
        Task<Wardrobe?> GetByAccountIdAsync(int accountId);
        Task<List<Account>> SearchAccountWithWardrobeAsync(string username, int limit = 5);
    }
}