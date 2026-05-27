using FluentValidation;
using SporticoApp.Application.DTOs.Wallets;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Mappings;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Services
{
    using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

    public class CoachWalletService : ICoachWalletService
    {
        private readonly ICoachRepository _coachRepository;
        private readonly ICoachWalletRepository _coachWalletRepository;
        private readonly IValidator<CoachWalletTransactionFilterRequest> _filterValidator;

        public CoachWalletService(
            ICoachRepository coachRepository,
            ICoachWalletRepository coachWalletRepository,
            IValidator<CoachWalletTransactionFilterRequest> filterValidator)
        {
            _coachRepository = coachRepository;
            _coachWalletRepository = coachWalletRepository;
            _filterValidator = filterValidator;
        }

        public async Task<Result<CoachWalletResponse>> GetMyWalletAsync(Guid coachId)
        {
            var coachExists = await _coachRepository.ExistsByUserIdAsync(coachId);
            if (!coachExists)
            {
                throw new ForbiddenException(
                    ErrorCodes.CoachProfileRequired,
                    "You must register as a coach first");
            }

            var wallet = await _coachWalletRepository.GetByCoachIdAsync(coachId);
            if (wallet == null)
            {
                wallet = new CoachWallet
                {
                    Id = Guid.NewGuid(),
                    CoachId = coachId,
                    AvailableBalance = 0,
                    PendingBalance = 0,
                    TotalEarned = 0,
                    TotalWithdrawn = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _coachWalletRepository.AddAsync(wallet);
            }

            return Result<CoachWalletResponse>.Success(wallet.ToResponse());
        }

        public async Task<Result<PagedResult<CoachWalletTransactionResponse>>> GetMyTransactionsAsync(
            Guid coachId,
            CoachWalletTransactionFilterRequest filter)
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

            var (items, totalCount) = await _coachWalletRepository.GetTransactionsPagedAsync(coachId, filter);

            var response = new PagedResult<CoachWalletTransactionResponse>(
                items.Select(x => x.ToResponse()).ToList(),
                totalCount,
                filter.PageNumber,
                filter.PageSize);

            return Result<PagedResult<CoachWalletTransactionResponse>>.Success(response);
        }
    }
}
