using FluentValidation;
using SporticoApp.Application.DTOs.Reviews;
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

    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly ICoachRepository _coachRepository;
        private readonly IValidator<CreateReviewRequest> _createValidator;
        private readonly IValidator<UpdateReviewRequest> _updateValidator;
        private readonly IValidator<ReviewFilterRequest> _filterValidator;

        public ReviewService(
            IReviewRepository reviewRepository,
            ICoachRepository coachRepository,
            IValidator<CreateReviewRequest> createValidator,
            IValidator<UpdateReviewRequest> updateValidator,
            IValidator<ReviewFilterRequest> filterValidator)
        {
            _reviewRepository = reviewRepository;
            _coachRepository = coachRepository;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _filterValidator = filterValidator;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Create — learner with a successful paid booking, one per coach
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<ReviewResponse>> CreateAsync(Guid learnerId, CreateReviewRequest request)
        {
            var validationResult = await _createValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    validationResult.Errors.Select(x => x.ErrorMessage).ToList());
            }

            if (request.CoachId == learnerId)
            {
                throw new ForbiddenException(
                    ErrorCodes.ReviewNotAllowed,
                    "You cannot review yourself");
            }

            var coachExists = await _coachRepository.ExistsByUserIdAsync(request.CoachId);
            if (!coachExists)
            {
                throw new NotFoundException(
                    ErrorCodes.CoachProfileNotFound,
                    "Coach not found");
            }

            // Eligibility: must have a successful paid booking (and the specific booking, if given).
            var eligible = await _reviewRepository.HasSuccessfulBookingForReviewAsync(
                learnerId, request.CoachId, request.BookingId);
            if (!eligible)
            {
                throw new ForbiddenException(
                    ErrorCodes.ReviewNotAllowed,
                    "You can only review a coach after a successful paid booking with them");
            }

            var existing = await _reviewRepository.GetByCoachAndLearnerForUpdateAsync(request.CoachId, learnerId);
            if (existing != null)
            {
                // The (coach, learner) unique constraint means at most one row ever exists.
                if (existing.Status == ReviewStatuses.Active)
                {
                    throw new ConflictException(
                        ErrorCodes.ReviewAlreadyExists,
                        "You already reviewed this coach. Update your existing review instead.");
                }

                if (existing.Status == ReviewStatuses.Hidden)
                {
                    throw new ConflictException(
                        ErrorCodes.ReviewNotAllowed,
                        "Your previous review for this coach was hidden by moderation.");
                }

                // Previously self-deleted → revive the same row (keeps the unique constraint intact).
                existing.Status = ReviewStatuses.Active;
                existing.Rating = request.Rating;
                existing.Comment = request.Comment?.Trim();
                existing.BookingId = request.BookingId;
                existing.DeletedAt = null;
                existing.DeletedByUserId = null;
                existing.ModerationReason = null;
                existing.UpdatedAt = DateTime.UtcNow;

                await _reviewRepository.SaveChangesAsync();
                await _reviewRepository.RecalculateCoachRatingAsync(request.CoachId);

                return await BuildOwnerResponseAsync(existing.Id, learnerId, request.CoachId);
            }

            var review = new Review
            {
                Id = Guid.NewGuid(),
                CoachId = request.CoachId,
                learner_id = learnerId,
                BookingId = request.BookingId,
                Rating = request.Rating,
                Comment = request.Comment?.Trim(),
                Status = ReviewStatuses.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _reviewRepository.AddAsync(review);
            await _reviewRepository.RecalculateCoachRatingAsync(request.CoachId);

            return await BuildOwnerResponseAsync(review.Id, learnerId, request.CoachId);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Update — owner only, active review, non-expired booking required
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<ReviewResponse>> UpdateAsync(Guid learnerId, Guid reviewId, UpdateReviewRequest request)
        {
            var validationResult = await _updateValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    validationResult.Errors.Select(x => x.ErrorMessage).ToList());
            }

            var review = await _reviewRepository.GetByIdForUpdateAsync(reviewId);
            if (review == null)
            {
                throw new NotFoundException(ErrorCodes.ReviewNotFound, "Review not found");
            }

            if (review.learner_id != learnerId)
            {
                throw new ForbiddenException(ErrorCodes.ReviewNotOwned, "You do not own this review");
            }

            if (review.Status != ReviewStatuses.Active)
            {
                throw new ConflictException(
                    ErrorCodes.ReviewNotAllowed,
                    "This review is not active and can no longer be edited.");
            }

            var canEdit = await _reviewRepository.HasNonExpiredSuccessfulBookingAsync(learnerId, review.CoachId);
            if (!canEdit)
            {
                throw new ConflictException(
                    ErrorCodes.ReviewEditExpired,
                    "Package has expired. Review can no longer be edited.");
            }

            review.Rating = request.Rating;
            review.Comment = request.Comment?.Trim();
            review.UpdatedAt = DateTime.UtcNow;

            await _reviewRepository.SaveChangesAsync();
            await _reviewRepository.RecalculateCoachRatingAsync(review.CoachId);

            return await BuildOwnerResponseAsync(review.Id, learnerId, review.CoachId);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Delete own — soft delete, recalculate stats
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<object>> DeleteOwnAsync(Guid learnerId, Guid reviewId)
        {
            var review = await _reviewRepository.GetByIdForUpdateAsync(reviewId);
            if (review == null)
            {
                throw new NotFoundException(ErrorCodes.ReviewNotFound, "Review not found");
            }

            if (review.learner_id != learnerId)
            {
                throw new ForbiddenException(ErrorCodes.ReviewNotOwned, "You do not own this review");
            }

            if (review.Status != ReviewStatuses.Deleted)
            {
                review.Status = ReviewStatuses.Deleted;
                review.DeletedAt = DateTime.UtcNow;
                review.DeletedByUserId = learnerId;
                review.UpdatedAt = DateTime.UtcNow;

                await _reviewRepository.SaveChangesAsync();
                await _reviewRepository.RecalculateCoachRatingAsync(review.CoachId);
            }

            return Result<object>.Success(new { status = "deleted" });
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public list — active reviews only, with CanEdit for the current user
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<PagedResult<ReviewResponse>>> GetCoachReviewsAsync(
            Guid? currentUserId,
            Guid coachId,
            ReviewFilterRequest filter)
        {
            var validationResult = await _filterValidator.ValidateAsync(filter);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    validationResult.Errors.Select(x => x.ErrorMessage).ToList());
            }

            var (items, totalCount) = await _reviewRepository.GetPagedByCoachAsync(coachId, filter);

            // At most one review belongs to the current user (one-per-coach), so a single
            // non-expired-booking check covers the whole page.
            var currentUserCanEdit = false;
            if (currentUserId.HasValue && items.Any(r => r.learner_id == currentUserId.Value))
            {
                currentUserCanEdit = await _reviewRepository.HasNonExpiredSuccessfulBookingAsync(
                    currentUserId.Value, coachId);
            }

            var responses = items
                .Select(r => r.ToResponse(
                    canEdit: currentUserId.HasValue
                        && r.learner_id == currentUserId.Value
                        && r.Status == ReviewStatuses.Active
                        && currentUserCanEdit))
                .ToList();

            var paged = new PagedResult<ReviewResponse>(
                responses, totalCount, filter.PageNumber, filter.PageSize);

            return Result<PagedResult<ReviewResponse>>.Success(paged);
        }

        public async Task<Result<CoachReviewSummaryResponse>> GetCoachReviewSummaryAsync(Guid coachId)
        {
            var stats = await _reviewRepository.GetRatingStatsByCoachAsync(coachId);

            var response = new CoachReviewSummaryResponse
            {
                CoachId = coachId,
                AverageRating = stats.AverageRating,
                TotalReviews = stats.TotalReviews,
                RatingBreakdown = new RatingBreakdown
                {
                    OneStar = stats.OneStar,
                    TwoStar = stats.TwoStar,
                    ThreeStar = stats.ThreeStar,
                    FourStar = stats.FourStar,
                    FiveStar = stats.FiveStar
                }
            };

            return Result<CoachReviewSummaryResponse>.Success(response);
        }

        public async Task<Result<ReviewResponse>> GetMyReviewForCoachAsync(Guid learnerId, Guid coachId)
        {
            var review = await _reviewRepository.GetByCoachAndLearnerAsync(coachId, learnerId);
            if (review == null || review.Status == ReviewStatuses.Deleted)
            {
                throw new NotFoundException(ErrorCodes.ReviewNotFound, "You have not reviewed this coach");
            }

            var canEdit = review.Status == ReviewStatuses.Active
                && await _reviewRepository.HasNonExpiredSuccessfulBookingAsync(learnerId, coachId);

            return Result<ReviewResponse>.Success(review.ToResponse(canEdit));
        }

        // Re-loads the review with navigations and computes the owner's CanEdit flag.
        private async Task<Result<ReviewResponse>> BuildOwnerResponseAsync(Guid reviewId, Guid learnerId, Guid coachId)
        {
            var saved = await _reviewRepository.GetByIdAsync(reviewId);
            var canEdit = saved != null
                && saved.Status == ReviewStatuses.Active
                && await _reviewRepository.HasNonExpiredSuccessfulBookingAsync(learnerId, coachId);

            return Result<ReviewResponse>.Success(saved!.ToResponse(canEdit));
        }
    }
}
