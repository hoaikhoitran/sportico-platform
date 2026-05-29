using SporticoApp.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface ICoachProfileMediaRepository
    {
        Task<List<CoachProfileMedia>> GetByCoachIdAsync(Guid coachId);

        Task<CoachProfileMedia?> GetByIdForUpdateAsync(Guid id);

        Task AddAsync(CoachProfileMedia media);

        Task UpdateAsync(CoachProfileMedia media);

        Task DeleteAsync(CoachProfileMedia media);
    }
}
