using FluentValidation;
using SporticoApp.Application.DTOs.Packages;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Mappings;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Services
{
    using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

    public class PackageService : IPackageService
    {
        private readonly IPackageRepository _packageRepository;
        private readonly IValidator<CreatePackageRequest> _createValidator;
        private readonly IValidator<UpdatePackageRequest> _updateValidator;
        private readonly IValidator<PackageFilterRequest> _filterValidator;

        public PackageService(
            IPackageRepository packageRepository,
            IValidator<CreatePackageRequest> createValidator,
            IValidator<UpdatePackageRequest> updateValidator,
            IValidator<PackageFilterRequest> filterValidator)
        {
            _packageRepository = packageRepository;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _filterValidator = filterValidator;
        }

        public async Task<Result<PackageResponse>> CreateAsync(
            CreatePackageRequest request)
        {
            var validationResult = await _createValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors
                    .Select(x => x.ErrorMessage)
                    .ToList();

                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    details);
            }

            if (await _packageRepository.ExistsByNameAsync(request.Name))
            {
                throw new ConflictException(
                    ErrorCodes.PackageNameAlreadyExists,
                    "Package name already exists");
            }

            var package = request.ToEntity();

            await _packageRepository.AddAsync(package);

            return Result<PackageResponse>.Success(package.ToResponse());
        }

        public async Task<Result<PagedResult<PackageResponse>>> GetPagedAsync(
            PackageFilterRequest filter)
        {
            var validationResult = await _filterValidator.ValidateAsync(filter);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors
                    .Select(x => x.ErrorMessage)
                    .ToList();

                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    details);
            }

            var (items, totalCount) = await _packageRepository.GetPagedAsync(
                filter.Keyword,
                filter.IsActive,
                filter.PageNumber,
                filter.PageSize);

            var response = new PagedResult<PackageResponse>(
                items.ToResponseList(),
                totalCount,
                filter.PageNumber,
                filter.PageSize);

            return Result<PagedResult<PackageResponse>>.Success(response);
        }

        public async Task<Result<PackageResponse>> GetByIdAsync(int id)
        {
            var package = await _packageRepository.GetByIdAsync(id);

            if (package == null)
            {
                throw new NotFoundException(
                    ErrorCodes.PackageNotFound,
                    "Package not found");
            }

            return Result<PackageResponse>.Success(package.ToResponse());
        }

        public async Task<Result<PackageResponse>> UpdateAsync(
            int id,
            UpdatePackageRequest request)
        {
            var validationResult = await _updateValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors
                    .Select(x => x.ErrorMessage)
                    .ToList();

                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    details);
            }

            var package = await _packageRepository.GetForUpdateByIdAsync(id);

            if (package == null)
            {
                throw new NotFoundException(
                    ErrorCodes.PackageNotFound,
                    "Package not found");
            }

            if (await _packageRepository.ExistsByNameExceptIdAsync(request.Name, id))
            {
                throw new ConflictException(
                    ErrorCodes.PackageNameAlreadyExists,
                    "Package name already exists");
            }

            package.ApplyUpdate(request);
            await _packageRepository.SaveChangesAsync();

            return Result<PackageResponse>.Success(package.ToResponse());
        }

        public async Task<Result<PackageResponse>> UpdateStatusAsync(
            int id,
            UpdatePackageStatusRequest request)
        {
            var package = await _packageRepository.GetForUpdateByIdAsync(id);

            if (package == null)
            {
                throw new NotFoundException(
                    ErrorCodes.PackageNotFound,
                    "Package not found");
            }

            package.IsActive = request.IsActive;
            await _packageRepository.SaveChangesAsync();

            return Result<PackageResponse>.Success(package.ToResponse());
        }
    }
}
