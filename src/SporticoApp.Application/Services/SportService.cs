using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentValidation;
using SporticoApp.Application.DTOs.Sports;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Mappings;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Services
{
    using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

    public class SportService : ISportService
    {
        private const string SlugPattern = "^[a-z0-9]+(?:-[a-z0-9]+)*$";

        private readonly ISportRepository _sportRepository;
        private readonly IValidator<CreateSportRequest> _validator;
        private readonly ISlugGenerator _slugGenerator;
        private readonly IValidator<SportFilterRequest> _filterValidator;

        public SportService(
            ISportRepository sportRepository,
            IValidator<CreateSportRequest> validator,
            ISlugGenerator slugGenerator,
            IValidator<SportFilterRequest> filterValidator)
        {
            _sportRepository = sportRepository;
            _validator = validator;
            _slugGenerator = slugGenerator;
            _filterValidator = filterValidator;
        }

        public async Task<Result<SportResponse>> CreateAsync(
            CreateSportRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var details = new List<string>();
                foreach (var error in validationResult.Errors)
                {
                    details.Add(error.ErrorMessage);
                }

                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    details);
            }

            var normalizedName = request.Name.Trim();
            var normalizedNameKey = normalizedName.ToLowerInvariant();

            var finalSlug = string.IsNullOrWhiteSpace(request.Slug)
                ? _slugGenerator.GenerateSlug(normalizedName)
                : request.Slug.Trim().ToLowerInvariant();

            if (!Regex.IsMatch(finalSlug, SlugPattern))
            {
                throw new ValidationException(
                    ErrorCodes.InvalidSportSlug,
                    "Slug format is invalid");
            }

            if (await _sportRepository.ExistsByNameAsync(normalizedNameKey))
            {
                throw new ConflictException(
                    ErrorCodes.SportNameAlreadyExists,
                    "Sport name already exists");
            }

            if (await _sportRepository.ExistsBySlugAsync(finalSlug))
            {
                throw new ConflictException(
                    ErrorCodes.SportSlugAlreadyExists,
                    "Sport slug already exists");
            }

            var sport = request.ToEntity(finalSlug);
            await _sportRepository.AddAsync(sport);

            return Result<SportResponse>.Success(sport.ToResponse());
        }

        public async Task<Result<SportResponse>> GetByIdAsync(int id)
        {
            var sport = await _sportRepository.GetByIdAsync(id);
            if (sport == null)
            {
                throw new NotFoundException(
                    ErrorCodes.SportNotFound,
                    "Sport not found");
            }

            return Result<SportResponse>.Success(sport.ToResponse());
        }

        public async Task<Result<PagedResult<SportResponse>>> GetPagedAsync(SportFilterRequest filter)
        {
            var validationResult = _filterValidator.Validate(filter);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors
                    .Select(e => e.ErrorMessage)
                    .ToList();
                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid filter parameters",
                    details);
            }
            var normalizedKeyword = string.IsNullOrWhiteSpace(filter.Keyword)
                ? null
                : filter.Keyword.Trim().ToLowerInvariant();
            var result = await _sportRepository.GetPagedAsync(
                normalizedKeyword, filter.IsActive, filter.PageNumber, filter.PageSize);
            var responseItems = result.Items.ToResponseList();
            var pagedResult = new PagedResult<SportResponse>(
                responseItems, filter.PageNumber, filter.PageSize, result.TotalCount);
            return Result<PagedResult<SportResponse>>.Success(pagedResult);
        }

        public async Task<Result<SportResponse>> UpdateStatusAsync(int id, UpdateSportStatusRequest request)
        {
            var sport = await _sportRepository.GetByIdAsync(id);
            if (sport == null)
            {
                throw new NotFoundException(
                    ErrorCodes.SportNotFound,
                    "Sport not found");
            }

            sport.IsActive = request.IsActive;
            await _sportRepository.SaveChangesAsync();

            return Result<SportResponse>.Success(sport.ToResponse());
        }
    }
}
