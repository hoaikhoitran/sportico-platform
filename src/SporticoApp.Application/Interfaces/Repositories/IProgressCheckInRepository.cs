using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface IProgressCheckInRepository
    {
        Task<ProgressCheckIn?> GetByIdAsync(Guid id);

        Task<ProgressCheckIn?> GetByIdForUpdateAsync(Guid id);

        Task<(List<ProgressCheckIn> Items, int TotalCount)> GetByBookingPagedAsync(
            Guid bookingId,
            int pageNumber,
            int pageSize);

        Task AddAsync(ProgressCheckIn checkIn);

        Task SaveChangesAsync();
    }
}
