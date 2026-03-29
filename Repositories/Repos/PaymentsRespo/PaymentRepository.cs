using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.PaymentsRespo
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly FashionDbContext _db;

        public PaymentRepository(FashionDbContext db)
        {
            _db = db;
        }

        public async Task<Payment?> GetByOrderCodeAsync(string orderCode)
        {
            return await _db.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.OrderCode == orderCode);
        }

        public async Task<Payment?> GetPaymentWithWalletAsync(string orderCode)
        {
            return await _db.Payments
                .Include(p => p.Account)
                    .ThenInclude(a => a.Wallet)
                .FirstOrDefaultAsync(p => p.OrderCode == orderCode);
        }

        public async Task AddAsync(Payment payment)
        {
            await _db.Payments.AddAsync(payment);
        }

        public void Update(Payment payment)
        {
            _db.Payments.Update(payment);
        }

        public IQueryable<Payment> Query()
        {
            return _db.Payments.AsQueryable();
        }
    }
}