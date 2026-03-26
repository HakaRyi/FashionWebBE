using Repositories.Entities;

namespace Repositories.Repos.PaymentsRespo
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetByOrderCodeAsync(string orderCode);
        Task<Payment?> GetPaymentWithWalletAsync(string orderCode);
        Task AddAsync(Payment payment);
        void Update(Payment payment);
        IQueryable<Payment> Query();
    }
}