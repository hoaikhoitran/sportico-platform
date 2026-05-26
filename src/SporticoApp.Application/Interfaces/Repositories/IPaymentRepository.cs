using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetByOrderCodeForUpdateAsync(long orderCode);

        Task<Payment?> GetLatestByReferenceAsync(
            string referenceType,
            Guid referenceId);

        Task<Payment?> GetLatestByReferenceForUpdateAsync(
            string referenceType,
            Guid referenceId);

        Task AddAsync(Payment payment);

        Task AddWithoutSaveAsync(Payment payment);

        Task SaveChangesAsync();
    }
}