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
        Task AddPaymentAsync(Payment payment);
        void UpdatePayment(Payment payment);
        Task<bool> SaveChangesAsync();
    }
}
