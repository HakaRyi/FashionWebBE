using Repositories.Entities;

namespace Repositories.Repos.Payments
{
    public interface IPaymentRepository
    {
        Task<IEnumerable<Package>> GetActivePackagesAsync();
        Task<Payment?> GetByOrderCodeAsync(string orderCode);
        Task<bool> SaveChangesAsync();
        Task<Payment?> GetPaymentWithWalletAsync(string orderCode);
        Task AddAsync(Payment payment);
        void Update(Payment payment);

        Task SaveTransaction(string appTransId, double amount);

        Task CreatePaymentAsync(Payment payment);
        Task UpdatePaymentAsync(Payment payment);
    }
}
