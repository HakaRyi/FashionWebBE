using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
