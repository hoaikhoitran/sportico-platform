using FluentValidation;
using SporticoApp.Application.DTOs.Vouchers;
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

    public class VoucherService : IVoucherService
    {
        private readonly IVoucherCampaignRepository _campaignRepository;
        private readonly IVoucherRedemptionRepository _redemptionRepository;
        private readonly ITrainingPackageRepository _trainingPackageRepository;
        private readonly IValidator<ValidateVoucherRequest> _validateValidator;
        private readonly IValidator<CreateVoucherCampaignRequest> _createValidator;
        private readonly IValidator<UpdateVoucherCampaignRequest> _updateValidator;
        private readonly IValidator<VoucherCampaignFilterRequest> _campaignFilterValidator;
        private readonly IValidator<VoucherRedemptionFilterRequest> _redemptionFilterValidator;

        public VoucherService(
            IVoucherCampaignRepository campaignRepository,
            IVoucherRedemptionRepository redemptionRepository,
            ITrainingPackageRepository trainingPackageRepository,
            IValidator<ValidateVoucherRequest> validateValidator,
            IValidator<CreateVoucherCampaignRequest> createValidator,
            IValidator<UpdateVoucherCampaignRequest> updateValidator,
            IValidator<VoucherCampaignFilterRequest> campaignFilterValidator,
            IValidator<VoucherRedemptionFilterRequest> redemptionFilterValidator)
        {
            _campaignRepository = campaignRepository;
            _redemptionRepository = redemptionRepository;
            _trainingPackageRepository = trainingPackageRepository;
            _validateValidator = validateValidator;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _campaignFilterValidator = campaignFilterValidator;
            _redemptionFilterValidator = redemptionFilterValidator;
        }

        public async Task<Result<VoucherQuoteResponse>> ValidateAsync(Guid learnerId, ValidateVoucherRequest request)
        {
            await ValidateOrThrowAsync(_validateValidator, request);

            var package = await _trainingPackageRepository.GetByIdAsync(request.TrainingPackageId);
            if (package == null)
            {
                throw new NotFoundException(ErrorCodes.TrainingPackageNotFound, "Training package not found");
            }

            var campaign = await _campaignRepository.GetByCodeAsync(request.Code.Trim());
            EnsureEligible(campaign, package.Price);
            await EnsureQuotaAsync(campaign!, learnerId);
            var discount = ComputeDiscount(campaign!, package.Price);
            EnsureBudget(campaign!, discount);

            return Result<VoucherQuoteResponse>.Success(new VoucherQuoteResponse
            {
                Code = campaign!.Code,
                OriginalAmount = package.Price,
                DiscountAmount = discount,
                TotalAmount = Math.Max(0, package.Price - discount),
                DiscountType = campaign.DiscountType,
                DiscountValue = campaign.DiscountValue,
                MaxDiscountAmount = campaign.MaxDiscountAmount
            });
        }

        public async Task<VoucherReservation?> ReserveForBookingAsync(
            Guid learnerId,
            string? voucherCode,
            Guid trainingPackageId,
            decimal originalAmount,
            Guid bookingId)
        {
            if (string.IsNullOrWhiteSpace(voucherCode))
            {
                return null;
            }

            var campaign = await _campaignRepository.GetByCodeForUpdateAsync(voucherCode.Trim());
            EnsureEligible(campaign, originalAmount);
            await EnsureQuotaAsync(campaign!, learnerId);
            var discount = ComputeDiscount(campaign!, originalAmount);
            EnsureBudget(campaign!, discount);

            var now = DateTime.UtcNow;
            campaign!.ReservedCount++;
            campaign.ReservedDiscountAmount += discount;
            campaign.UpdatedAt = now;

            var redemption = new VoucherRedemption
            {
                Id = Guid.NewGuid(),
                VoucherCampaignId = campaign.Id,
                BookingId = bookingId,
                LearnerId = learnerId,
                Status = VoucherRedemptionStatuses.Reserved,
                OriginalAmount = originalAmount,
                DiscountAmount = discount,
                ReservedAt = now,
                // Reservation window mirrors the PayOS payment-link expiry so a background sweep can
                // safely release abandoned reservations even if webhook/reconcile never fire.
                ExpiresAt = now.AddMinutes(30),
                CreatedAt = now,
                UpdatedAt = now
            };

            await _redemptionRepository.AddWithoutSaveAsync(redemption);

            return new VoucherReservation
            {
                RedemptionId = redemption.Id,
                VoucherCampaignId = campaign.Id,
                DiscountAmount = discount,
                CodeSnapshot = campaign.Code,
                DiscountTypeSnapshot = campaign.DiscountType,
                DiscountValueSnapshot = campaign.DiscountValue,
                MaxDiscountAmountSnapshot = campaign.MaxDiscountAmount
            };
        }

        public async Task ApplyForBookingAsync(Guid bookingId, Guid? paymentId)
        {
            var redemption = await _redemptionRepository.GetByBookingIdForUpdateAsync(bookingId);
            if (redemption == null || redemption.Status != VoucherRedemptionStatuses.Reserved)
            {
                return; // idempotent no-op: no voucher, or already applied/released
            }

            var campaign = await _campaignRepository.GetByIdForUpdateAsync(redemption.VoucherCampaignId);

            var now = DateTime.UtcNow;
            redemption.Status = VoucherRedemptionStatuses.Applied;
            redemption.PaymentId = paymentId;
            redemption.AppliedAt = now;
            redemption.UpdatedAt = now;

            if (campaign != null)
            {
                campaign.ReservedCount = Math.Max(0, campaign.ReservedCount - 1);
                campaign.UsedCount += 1;
                campaign.ReservedDiscountAmount = Math.Max(0, campaign.ReservedDiscountAmount - redemption.DiscountAmount);
                campaign.UsedDiscountAmount += redemption.DiscountAmount;
                campaign.UpdatedAt = now;
            }
        }

        public async Task ReleaseForBookingAsync(Guid bookingId, string releaseReason)
        {
            var redemption = await _redemptionRepository.GetByBookingIdForUpdateAsync(bookingId);
            if (redemption == null || redemption.Status != VoucherRedemptionStatuses.Reserved)
            {
                return; // idempotent no-op: no voucher, already released, or (never) an applied one
            }

            var campaign = await _campaignRepository.GetByIdForUpdateAsync(redemption.VoucherCampaignId);

            var now = DateTime.UtcNow;
            redemption.Status = VoucherRedemptionStatuses.Released;
            redemption.ReleasedAt = now;
            redemption.ReleaseReason = releaseReason;
            redemption.UpdatedAt = now;

            if (campaign != null)
            {
                campaign.ReservedCount = Math.Max(0, campaign.ReservedCount - 1);
                campaign.ReservedDiscountAmount = Math.Max(0, campaign.ReservedDiscountAmount - redemption.DiscountAmount);
                campaign.UpdatedAt = now;
            }
        }

        // ── Admin campaign management ──────────────────────────────────────────

        public async Task<Result<VoucherCampaignResponse>> CreateCampaignAsync(Guid adminUserId, CreateVoucherCampaignRequest request)
        {
            await ValidateOrThrowAsync(_createValidator, request);

            var code = request.Code.Trim();
            if (await _campaignRepository.ExistsByCodeAsync(code))
            {
                throw new ConflictException(ErrorCodes.VoucherCodeAlreadyExists, $"Voucher code '{code}' already exists");
            }

            var now = DateTime.UtcNow;
            var campaign = new VoucherCampaign
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = request.Name.Trim(),
                Description = request.Description,
                DiscountType = request.DiscountType,
                DiscountValue = request.DiscountValue,
                MaxDiscountAmount = request.MaxDiscountAmount,
                MinOrderAmount = request.MinOrderAmount,
                StartAt = request.StartAt,
                EndAt = request.EndAt,
                Status = VoucherCampaignStatuses.Draft,
                MaxUsesTotal = request.MaxUsesTotal,
                MaxUsesPerLearner = request.MaxUsesPerLearner,
                BudgetAmount = request.BudgetAmount,
                ReservedCount = 0,
                UsedCount = 0,
                ReservedDiscountAmount = 0,
                UsedDiscountAmount = 0,
                CreatedByUserId = adminUserId,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _campaignRepository.AddWithoutSaveAsync(campaign);
            await _campaignRepository.SaveChangesAsync();

            return Result<VoucherCampaignResponse>.Success(campaign.ToResponse());
        }

        public async Task<Result<VoucherCampaignResponse>> UpdateCampaignAsync(
            Guid adminUserId, Guid campaignId, UpdateVoucherCampaignRequest request)
        {
            await ValidateOrThrowAsync(_updateValidator, request);

            var campaign = await _campaignRepository.GetByIdForUpdateAsync(campaignId);
            if (campaign == null)
            {
                throw new NotFoundException(ErrorCodes.VoucherCampaignNotFound, "Voucher campaign not found");
            }

            var hasRedemptions = await _campaignRepository.HasAnyRedemptionAsync(campaignId);

            // Financial fields are locked once the campaign has been redeemed at least once, so past
            // redemptions stay historically accurate. Admins must end this campaign and create a new
            // one instead of editing the economics of an already-used voucher.
            if (hasRedemptions &&
                (request.DiscountType != null || request.DiscountValue.HasValue ||
                 request.MaxDiscountAmount.HasValue || request.MinOrderAmount.HasValue))
            {
                throw new ConflictException(
                    ErrorCodes.VoucherCampaignHasRedemptions,
                    "This campaign already has redemptions; its discount fields can no longer be edited. End it and create a new campaign instead.");
            }

            if (!hasRedemptions)
            {
                if (request.DiscountType != null) campaign.DiscountType = request.DiscountType;
                if (request.DiscountValue.HasValue) campaign.DiscountValue = request.DiscountValue.Value;
                if (request.MaxDiscountAmount.HasValue) campaign.MaxDiscountAmount = request.MaxDiscountAmount;
                if (request.MinOrderAmount.HasValue) campaign.MinOrderAmount = request.MinOrderAmount;
            }

            if (request.Name != null) campaign.Name = request.Name.Trim();
            if (request.Description != null) campaign.Description = request.Description;
            if (request.StartAt.HasValue) campaign.StartAt = request.StartAt;
            if (request.EndAt.HasValue) campaign.EndAt = request.EndAt;
            if (request.MaxUsesTotal.HasValue) campaign.MaxUsesTotal = request.MaxUsesTotal;
            if (request.MaxUsesPerLearner.HasValue) campaign.MaxUsesPerLearner = request.MaxUsesPerLearner;
            if (request.BudgetAmount.HasValue) campaign.BudgetAmount = request.BudgetAmount;

            if (campaign.StartAt.HasValue && campaign.EndAt.HasValue && campaign.StartAt >= campaign.EndAt)
            {
                throw new ValidationException(ErrorCodes.VoucherInvalidDateRange, "startAt must be before endAt");
            }

            campaign.UpdatedByUserId = adminUserId;
            campaign.UpdatedAt = DateTime.UtcNow;

            await _campaignRepository.SaveChangesAsync();

            return Result<VoucherCampaignResponse>.Success(campaign.ToResponse());
        }

        public Task<Result<VoucherCampaignResponse>> ActivateCampaignAsync(Guid adminUserId, Guid campaignId)
            => TransitionStatusAsync(adminUserId, campaignId, VoucherCampaignStatuses.Active);

        public Task<Result<VoucherCampaignResponse>> PauseCampaignAsync(Guid adminUserId, Guid campaignId)
            => TransitionStatusAsync(adminUserId, campaignId, VoucherCampaignStatuses.Paused);

        public Task<Result<VoucherCampaignResponse>> EndCampaignAsync(Guid adminUserId, Guid campaignId)
            => TransitionStatusAsync(adminUserId, campaignId, VoucherCampaignStatuses.Ended);

        private async Task<Result<VoucherCampaignResponse>> TransitionStatusAsync(Guid adminUserId, Guid campaignId, string newStatus)
        {
            var campaign = await _campaignRepository.GetByIdForUpdateAsync(campaignId);
            if (campaign == null)
            {
                throw new NotFoundException(ErrorCodes.VoucherCampaignNotFound, "Voucher campaign not found");
            }

            if (campaign.Status == VoucherCampaignStatuses.Ended)
            {
                throw new ConflictException(ErrorCodes.VoucherCampaignAlreadyEnded, "An ended campaign cannot be reactivated");
            }

            campaign.Status = newStatus;
            campaign.UpdatedByUserId = adminUserId;
            campaign.UpdatedAt = DateTime.UtcNow;

            await _campaignRepository.SaveChangesAsync();

            return Result<VoucherCampaignResponse>.Success(campaign.ToResponse());
        }

        public async Task<Result<PagedResult<VoucherCampaignResponse>>> GetCampaignsAsync(VoucherCampaignFilterRequest filter)
        {
            await ValidateOrThrowAsync(_campaignFilterValidator, filter);

            var (items, totalCount) = await _campaignRepository.GetPagedAsync(filter);

            return Result<PagedResult<VoucherCampaignResponse>>.Success(new PagedResult<VoucherCampaignResponse>(
                items.Select(x => x.ToResponse()).ToList(), totalCount, filter.PageNumber, filter.PageSize));
        }

        public async Task<Result<VoucherCampaignResponse>> GetCampaignByIdAsync(Guid campaignId)
        {
            var campaign = await _campaignRepository.GetByIdAsync(campaignId);
            if (campaign == null)
            {
                throw new NotFoundException(ErrorCodes.VoucherCampaignNotFound, "Voucher campaign not found");
            }

            return Result<VoucherCampaignResponse>.Success(campaign.ToResponse());
        }

        public async Task<Result<PagedResult<VoucherRedemptionResponse>>> GetRedemptionsAsync(
            Guid campaignId, VoucherRedemptionFilterRequest filter)
        {
            await ValidateOrThrowAsync(_redemptionFilterValidator, filter);

            var campaign = await _campaignRepository.GetByIdAsync(campaignId);
            if (campaign == null)
            {
                throw new NotFoundException(ErrorCodes.VoucherCampaignNotFound, "Voucher campaign not found");
            }

            var (items, totalCount) = await _redemptionRepository.GetPagedByCampaignAsync(campaignId, filter);

            return Result<PagedResult<VoucherRedemptionResponse>>.Success(new PagedResult<VoucherRedemptionResponse>(
                items.Select(x => x.ToResponse()).ToList(), totalCount, filter.PageNumber, filter.PageSize));
        }

        // ── Shared rule evaluation ──────────────────────────────────────────────

        private static void EnsureEligible(VoucherCampaign? campaign, decimal originalAmount)
        {
            if (campaign == null)
            {
                throw new NotFoundException(ErrorCodes.VoucherNotFound, "Voucher not found");
            }

            if (campaign.Status == VoucherCampaignStatuses.Paused || campaign.Status == VoucherCampaignStatuses.Draft)
            {
                throw new ConflictException(ErrorCodes.VoucherNotActive, "This voucher is not currently active");
            }

            if (campaign.Status == VoucherCampaignStatuses.Ended)
            {
                throw new ConflictException(ErrorCodes.VoucherExpired, "This voucher has ended");
            }

            var now = DateTime.UtcNow;
            if (campaign.StartAt.HasValue && now < campaign.StartAt.Value)
            {
                throw new ConflictException(ErrorCodes.VoucherNotStarted, "This voucher has not started yet");
            }

            if (campaign.EndAt.HasValue && now > campaign.EndAt.Value)
            {
                throw new ConflictException(ErrorCodes.VoucherExpired, "This voucher has expired");
            }

            if (campaign.MinOrderAmount.HasValue && originalAmount < campaign.MinOrderAmount.Value)
            {
                throw new ConflictException(
                    ErrorCodes.VoucherMinOrderNotMet,
                    $"This voucher requires a minimum order of {campaign.MinOrderAmount.Value}");
            }
        }

        private async Task EnsureQuotaAsync(VoucherCampaign campaign, Guid learnerId)
        {
            // Reserved uses count toward the quota exactly like applied ones, so two learners cannot
            // both pass validation for the last available use.
            if (campaign.MaxUsesTotal.HasValue &&
                campaign.ReservedCount + campaign.UsedCount >= campaign.MaxUsesTotal.Value)
            {
                throw new ConflictException(ErrorCodes.VoucherUsageLimitReached, "This voucher has reached its usage limit");
            }

            if (campaign.MaxUsesPerLearner.HasValue)
            {
                var learnerCount = await _redemptionRepository.CountByLearnerAndCampaignAsync(
                    learnerId,
                    campaign.Id,
                    new[] { VoucherRedemptionStatuses.Reserved, VoucherRedemptionStatuses.Applied });

                if (learnerCount >= campaign.MaxUsesPerLearner.Value)
                {
                    throw new ConflictException(ErrorCodes.VoucherLearnerLimitReached, "You have already used this voucher the maximum number of times");
                }
            }
        }

        private static void EnsureBudget(VoucherCampaign campaign, decimal discount)
        {
            if (campaign.BudgetAmount.HasValue &&
                campaign.ReservedDiscountAmount + campaign.UsedDiscountAmount + discount > campaign.BudgetAmount.Value)
            {
                throw new ConflictException(ErrorCodes.VoucherBudgetExceeded, "This voucher's budget has been exhausted");
            }
        }

        private static decimal ComputeDiscount(VoucherCampaign campaign, decimal originalAmount)
        {
            decimal discount;

            if (campaign.DiscountType == VoucherDiscountTypes.FixedAmount)
            {
                discount = Math.Min(campaign.DiscountValue, originalAmount);
            }
            else
            {
                discount = originalAmount * campaign.DiscountValue / 100m;
                if (campaign.MaxDiscountAmount.HasValue)
                {
                    discount = Math.Min(discount, campaign.MaxDiscountAmount.Value);
                }
            }

            if (discount < 0) discount = 0;
            if (discount > originalAmount) discount = originalAmount;

            return discount;
        }

        private static async Task ValidateOrThrowAsync<T>(IValidator<T> validator, T instance)
        {
            var result = await validator.ValidateAsync(instance);
            if (!result.IsValid)
            {
                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    result.Errors.Select(x => x.ErrorMessage).ToList());
            }
        }
    }
}
