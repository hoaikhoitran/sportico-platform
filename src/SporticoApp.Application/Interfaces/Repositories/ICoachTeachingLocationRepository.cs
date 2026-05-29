using SporticoApp.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface ICoachTeachingLocationRepository
    {
        Task<List<CoachTeachingLocation>> GetByCoachIdAsync(Guid coachId);

        Task<CoachTeachingLocation?> GetByIdForUpdateAsync(Guid id);

        Task AddAsync(CoachTeachingLocation location);

        Task UpdateAsync(CoachTeachingLocation location);

        Task DeleteAsync(CoachTeachingLocation location);

        /// <summary>
        /// Sets IsDefault = false for all of the coach's locations except the given id.
        /// </summary>
        Task ClearDefaultsAsync(Guid coachId, Guid exceptId);
    }
}
