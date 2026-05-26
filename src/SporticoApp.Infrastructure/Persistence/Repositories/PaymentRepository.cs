using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly AppDbContext _context;

        public PaymentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Payment?> GetByOrderCodeForUpdateAsync(long orderCode)
        {
            return await _context.Payments
                .FirstOrDefaultAsync(x => x.OrderCode == orderCode);
        }

        public async Task<Payment?> GetLatestByReferenceAsync(
            string referenceType,
            Guid referenceId)
        {
            return await _context.Payments
                .AsNoTracking()
                .Where(x => x.ReferenceType == referenceType &&
                            x.ReferenceId == referenceId)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<Payment?> GetLatestByReferenceForUpdateAsync(
            string referenceType,
            Guid referenceId)
        {
            return await _context.Payments
                .Where(x => x.ReferenceType == referenceType &&
                            x.ReferenceId == referenceId)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task AddAsync(Payment payment)
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
        }

        public Task AddWithoutSaveAsync(Payment payment)
        {
            _context.Payments.Add(payment);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}