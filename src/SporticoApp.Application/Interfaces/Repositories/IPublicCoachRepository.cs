using SporticoApp.Application.DTOs.PublicCoaches;
using SporticoApp.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface IPublicCoachRepository
    {
        Task<(List<CoachProfile> Items, int TotalCount)> GetPagedAsync(
            PublicCoachFilterRequest filter);

        Task<CoachProfile?> GetByIdAsync(Guid coachId);
    }
}
