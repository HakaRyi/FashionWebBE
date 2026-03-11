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

        public async Task AddPaymentAsync(Payment payment) => await _db.Payments.AddAsync(payment);

        public void UpdatePayment(Payment payment) => _db.Payments.Update(payment);

        public async Task<bool> SaveChangesAsync() => (await _db.SaveChangesAsync()) > 0;
    }
}
