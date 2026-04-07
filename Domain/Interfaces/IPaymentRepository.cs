using Domain.Entities;

namespace Domain.Interfaces

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