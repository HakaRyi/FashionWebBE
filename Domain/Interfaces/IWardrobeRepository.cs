using Domain.Entities;

namespace Domain.Interfaces

{
    public interface IWardrobeRepository
    {
        Task<int> CreateWardrobe(Wardrobe wardrobe);
        Task<List<Wardrobe>> GetAll();
        Task<Wardrobe?> GetByIdAsync(int wardrobeId);
        Task<Wardrobe?> GetByAccountIdAsync(int accountId);
    }
}