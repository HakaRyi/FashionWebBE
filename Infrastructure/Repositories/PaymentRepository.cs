using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace Infrastructure.Repositories
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

        public async Task<Payment?> GetPaymentByOrderCodeAsync(string orderCode)
        {
            return await _db.Set<Payment>()
                .FirstOrDefaultAsync(p => p.OrderCode == orderCode);
        }
    }
}