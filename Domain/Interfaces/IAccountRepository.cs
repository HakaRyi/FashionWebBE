using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IAccountRepository
    {
        Task<Account?> GetAccountWithProfileAndAvatarsAsync(int accountId);
        Task<List<Account>> GetFashionExperts();
        Task<List<Account>> GetAll();
        Task<Account> GetAccountById(int userId);
        Task AddRefreshTokenAsync(RefreshToken refreshToken);
        Task<RefreshToken?> GetRefreshTokenByAccountIdAsync(int accountId);
        Task UpdateRefreshTokenAsync(RefreshToken token);
        Task<int> UpdateAccount(Account account);
        Task<RefreshToken?> GetRefreshTokenByTokenAsync(string token);
        Task<Account?> GetFullAccountDetailsAsync(int accountId);
    }
}
