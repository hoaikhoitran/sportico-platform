using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SporticoApp.Application.DTOs.Packages;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IPackageService
    {
        Task<Result<PackageResponse>> CreateAsync(CreatePackageRequest request);

        Task<Result<PagedResult<PackageResponse>>> GetPagedAsync(
            PackageFilterRequest filter);

        Task<Result<PackageResponse>> GetByIdAsync(int id);

        Task<Result<PackageResponse>> UpdateAsync(
            int id,
            UpdatePackageRequest request);

        Task<Result<PackageResponse>> UpdateStatusAsync(
            int id,
            UpdatePackageStatusRequest request);
    }
}
