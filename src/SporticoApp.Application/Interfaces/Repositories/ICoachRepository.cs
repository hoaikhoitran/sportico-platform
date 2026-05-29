using SporticoApp.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface ICoachRepository
    {
        Task<bool> ExistsByUserIdAsync(Guid userId);

        Task<CoachProfile?> GetByUserIdAsync(Guid userId);

        Task<CoachProfile?> GetByUserIdWithDetailsAsync(Guid userId);

        Task<CoachProfile?> GetByUserIdForUpdateAsync(Guid userId);

        Task SaveChangesAsync();

        Task CreateCoachProfileAsync(
            CoachProfile coachProfile,
            int coachRoleId,
            List<int> sportIds);
    }
}
