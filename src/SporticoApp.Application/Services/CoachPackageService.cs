using FluentValidation;
using SporticoApp.Application.DTOs.CoachPackages;
using SporticoApp.Application.DTOs.Packages;
using SporticoApp.Application.DTOs.Payments;
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

    public class CoachPackageService : ICoachPackageService
    {
        private readonly ICoachRepository _coachRepository;
        private readonly IPackageRepository _packageRepository;
        private readonly ICoachPackageRepository _coachPackageRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPayOsService _payOsService;
        private readonly IValidator<PurchaseCoachPackageRequest> _validator;

        public CoachPackageService(
            ICoachRepository coachRepository,
            IPackageRepository packageRepository,
            ICoachPackageRepository coachPackageRepository,
            IPaymentRepository paymentRepository,
            IPayOsService payOsService,
            IValidator<PurchaseCoachPackageRequest> validator)
        {
            _coachRepository = coachRepository;
            _packageRepository = packageRepository;
            _coachPackageRepository = coachPackageRepository;
            _paymentRepository = paymentRepository;
            _payOsService = payOsService;
            _validator = validator;
        }

        public async Task<Result<PurchaseCoachPackagePayOsResponse>> PurchaseWithPayOsAsync(
            Guid coachId,
            PurchaseCoachPackageRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);

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

            var coachExists =
                await _coachRepository.ExistsByUserIdAsync(coachId);

            if (!coachExists)
            {
                throw new ForbiddenException(
                    ErrorCodes.CoachProfileRequired,
                    "You must register as a coach first");
            }

            var package = await _packageRepository.GetByIdAsync(request.PackageId);

            if (package == null)
            {
                throw new NotFoundException(
                    ErrorCodes.PackageNotFound,
                    "Package not found");
            }

            if (!package.IsActive)
            {
                throw new ConflictException(
                    ErrorCodes.PackageInactive,
                    "Package is inactive");
            }

            if (package.Price <= 0 || package.Price % 1 != 0)
            {
                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Package price must be a positive whole number");
            }

            var current =
                await _coachPackageRepository.GetCurrentForUpdateAsync(coachId);

            var now = DateTime.UtcNow;

            if (current != null)
            {
                // Dev note: if local DB has dirty pending coach_packages, cancel manually:
                // UPDATE coach_packages SET status = 'cancelled' WHERE status = 'pending';
                var stillUsable =
                    current.Status == CoachPackageStatuses.Active &&
                    current.EndDate > now &&
                    current.RemainingPosts > 0;

                if (stillUsable)
                {
                    throw new ConflictException(
                        ErrorCodes.CoachPackageStillActive,
                        "You still have an active package");
                }

                if (current.Status == CoachPackageStatuses.Pending)
                {
                    var pendingPayment =
                        await _paymentRepository.GetLatestByReferenceForUpdateAsync(
                            PaymentReferenceTypes.CoachPackage,
                            current.Id);

                    var isPending = pendingPayment?.Status == PaymentStatuses.Pending;
                    var notExpired = pendingPayment?.ExpiredAt == null ||
                                     pendingPayment.ExpiredAt > now;

                    if (isPending && notExpired)
                    {
                        throw new ConflictException(
                            ErrorCodes.CoachPackagePendingPayment,
                            "You already have a pending package payment");
                    }

                    current.Status = CoachPackageStatuses.Cancelled;
                    if (pendingPayment != null && isPending && !notExpired)
                    {
                        pendingPayment.Status = PaymentStatuses.Cancelled;
                    }

                    await _coachPackageRepository.SaveChangesAsync();
                }

                if (current.EndDate <= now || current.RemainingPosts <= 0)
                {
                    current.Status = CoachPackageStatuses.Expired;
                    await _coachPackageRepository.SaveChangesAsync();
                }
            }

            var orderCode =
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var coachPackage = package.ToPendingCoachPackage(coachId);

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                UserId = coachId,
                Amount = package.Price,
                Method = PaymentMethods.PayOs,
                ReferenceType = PaymentReferenceTypes.CoachPackage,
                ReferenceId = coachPackage.Id,
                Status = PaymentStatuses.Pending,
                TransactionCode = orderCode.ToString(),
                OrderCode = orderCode,
                CreatedAt = now
            };

            var payOsResult =
                await _payOsService.CreatePaymentLinkAsync(
                    new CreatePayOsPaymentRequest
                    {
                        OrderCode = orderCode,
                        Amount = (int)package.Price,
                        Description = $"SPT{orderCode}",
                        BuyerName = "Sportico Coach"
                    });

            payment.PaymentLinkId = payOsResult.PaymentLinkId;
            payment.CheckoutUrl = payOsResult.CheckoutUrl;
            payment.ExpiredAt = payOsResult.ExpiredAt;

            await _coachPackageRepository.AddWithoutSaveAsync(coachPackage);
            await _paymentRepository.AddWithoutSaveAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            var response = new PurchaseCoachPackagePayOsResponse
            {
                CoachPackageId = coachPackage.Id,
                PaymentId = payment.Id,
                OrderCode = orderCode,
                CheckoutUrl = payOsResult.CheckoutUrl,
                Status = PaymentStatuses.Pending,
                ExpiredAt = payOsResult.ExpiredAt
            };

            return Result<PurchaseCoachPackagePayOsResponse>.Success(response);
        }

        public async Task<Result<CoachPackageResponse>> PurchaseManualAsync(
            Guid coachId,
            PurchaseCoachPackageRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);

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

            var coachExists =
                await _coachRepository.ExistsByUserIdAsync(coachId);

            if (!coachExists)
            {
                throw new ForbiddenException(
                    ErrorCodes.CoachProfileRequired,
                    "You must register as a coach first");
            }

            var package = await _packageRepository.GetByIdAsync(request.PackageId);

            if (package == null)
            {
                throw new NotFoundException(
                    ErrorCodes.PackageNotFound,
                    "Package not found");
            }

            if (!package.IsActive)
            {
                throw new ConflictException(
                    ErrorCodes.PackageInactive,
                    "Package is inactive");
            }

            var current =
                await _coachPackageRepository.GetCurrentForUpdateAsync(coachId);

            var now = DateTime.UtcNow;

            if (current != null)
            {
                var stillUsable =
                    current.Status == CoachPackageStatuses.Active &&
                    current.EndDate > now &&
                    current.RemainingPosts > 0;

                if (stillUsable)
                {
                    throw new ConflictException(
                        ErrorCodes.CoachPackageStillActive,
                        "You still have an active package");
                }

                if (current.Status == CoachPackageStatuses.Pending)
                {
                    var pendingPayment =
                        await _paymentRepository.GetLatestByReferenceAsync(
                            PaymentReferenceTypes.CoachPackage,
                            current.Id);

                    var isPending = pendingPayment?.Status == PaymentStatuses.Pending;
                    var notExpired = pendingPayment?.ExpiredAt == null ||
                                     pendingPayment.ExpiredAt > now;

                    if (isPending && notExpired)
                    {
                        throw new ConflictException(
                            ErrorCodes.CoachPackagePendingPayment,
                            "You already have a pending package payment");
                    }

                    current.Status = CoachPackageStatuses.Cancelled;
                    await _coachPackageRepository.SaveChangesAsync();
                }

                if (current.EndDate <= now || current.RemainingPosts <= 0)
                {
                    current.Status = CoachPackageStatuses.Expired;
                    await _coachPackageRepository.SaveChangesAsync();
                }
            }

            var coachPackage = package.ToActiveCoachPackage(coachId);

            await _coachPackageRepository.AddAsync(coachPackage);

            return Result<CoachPackageResponse>.Success(coachPackage.ToResponse());
        }

        public async Task<Result<CoachPackageResponse>> GetCurrentAsync(
            Guid coachId)
        {
            var current =
                await _coachPackageRepository.GetCurrentByCoachIdAsync(coachId);

            if (current == null)
            {
                throw new NotFoundException(
                    ErrorCodes.CoachPackageNotFound,
                    "Current coach package not found");
            }

            return Result<CoachPackageResponse>.Success(
                current.ToResponse());
        }

        public async Task<Result<PagedResult<CoachPackageResponse>>> GetHistoryAsync(
            Guid coachId,
            CoachPackageHistoryFilterRequest filter)
        {
            var (items, totalCount) = await _coachPackageRepository.GetHistoryAsync(
                coachId,
                filter.PageNumber,
                filter.PageSize);

            var response = new PagedResult<CoachPackageResponse>(
                items.Select(x => x.ToResponse()).ToList(),
                totalCount,
                filter.PageNumber,
                filter.PageSize);

            return Result<PagedResult<CoachPackageResponse>>.Success(response);
        }
    }
}