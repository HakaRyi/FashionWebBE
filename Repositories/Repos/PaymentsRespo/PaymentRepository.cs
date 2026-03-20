using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.Payments
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly FashionDbContext _db;
        public PaymentRepository(FashionDbContext db) => _db = db;

        public async Task<IEnumerable<Package>> GetActivePackagesAsync()
            => await _db.Packages.Where(p => p.IsActive == true).ToListAsync();

        public async Task<Payment?> GetByOrderCodeAsync(string orderCode)
            => await _db.Payments.Include(p => p.Package).FirstOrDefaultAsync(p => p.OrderCode == orderCode);

        public async Task AddAsync(Payment payment) => await _db.Payments.AddAsync(payment);

        public void Update(Payment payment) => _db.Payments.Update(payment);

        public async Task<bool> SaveChangesAsync() => (await _db.SaveChangesAsync()) > 0;

        public async Task<Payment?> GetPaymentWithWalletAsync(string orderCode)
        {
            return await _db.Payments
                .Include(p => p.Package)
                .Include(p => p.Account)
                    .ThenInclude(a => a.Wallet)
                .FirstOrDefaultAsync(p => p.OrderCode == orderCode);
        }

        public async Task CreatePaymentAsync(Payment payment)
        {
            await _db.Payments.AddAsync(payment);
            await _db.SaveChangesAsync();
        }

        public async Task UpdatePaymentAsync(Payment payment)
        {
            _db.Payments.Update(payment);
            await _db.SaveChangesAsync();
        }

        public Task SaveTransaction(string appTransId, double amount)
        {
            throw new NotImplementedException();
        }
    }
}
