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

    public class ReviewReportService : IReviewReportService
    {
        private const int MaxPageSize = 50;

        private readonly IReviewReportRepository _reportRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IValidator<CreateReviewReportRequest> _createValidator;
        private readonly IValidator<ResolveReviewReportRequest> _resolveValidator;

        public ReviewReportService(
            IReviewReportRepository reportRepository,
            IReviewRepository reviewRepository,
            IValidator<CreateReviewReportRequest> createValidator,
            IValidator<ResolveReviewReportRequest> resolveValidator)
        {
            _reportRepository = reportRepository;
            _reviewRepository = reviewRepository;
            _createValidator = createValidator;
            _resolveValidator = resolveValidator;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Coach reports a review about themselves
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<ReviewReportResponse>> ReportAsync(
            Guid coachId,
            Guid reviewId,
            CreateReviewReportRequest request)
        {
            var validationResult = await _createValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    validationResult.Errors.Select(x => x.ErrorMessage).ToList());
            }

            var review = await _reviewRepository.GetByIdAsync(reviewId);
            if (review == null)
            {
                throw new NotFoundException(ErrorCodes.ReviewNotFound, "Review not found");
            }

            // Only the reviewed coach can report; a coach cannot report another coach's review.
            if (review.CoachId != coachId)
            {
                throw new ForbiddenException(
                    ErrorCodes.ReviewReportNotAllowed,
                    "You can only report reviews written about you");
            }

            if (review.Status != ReviewStatuses.Active)
            {
                throw new ConflictException(
                    ErrorCodes.ReviewReportNotAllowed,
                    "This review is not active and cannot be reported");
            }

            var openReport = await _reportRepository.GetOpenReportAsync(reviewId, coachId);
            if (openReport != null)
            {
                throw new ConflictException(
                    ErrorCodes.ReviewReportNotAllowed,
                    "You already have an open report for this review");
            }

            var report = new Report
            {
                Id = Guid.NewGuid(),
                reporter_id = coachId,
                target_user_id = null,
                TargetType = ReportTargetTypes.Review,
                TargetId = reviewId,
                Reason = request.Reason.Trim(),
                Description = request.Description?.Trim(),
                Status = ReportStatuses.Pending,
                ActionTaken = ReportActions.None,
                CreatedAt = DateTime.UtcNow
            };

            await _reportRepository.AddAsync(report);

            return Result<ReviewReportResponse>.Success(report.ToReviewReportResponse(review));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Admin: list review reports
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<PagedResult<ReviewReportResponse>>> GetReportsAsync(
            ReviewReportFilterRequest filter)
        {
            if (filter.PageNumber < 1)
            {
                throw new ValidationException(ErrorCodes.ValidationError, "Page number must be greater than 0");
            }

            if (filter.PageSize < 1 || filter.PageSize > MaxPageSize)
            {
                throw new ValidationException(ErrorCodes.ValidationError, $"Page size must be between 1 and {MaxPageSize}");
            }

            if (!string.IsNullOrWhiteSpace(filter.Status) &&
                !ReportStatuses.All.Contains(filter.Status.Trim().ToLowerInvariant()))
            {
                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid report status filter",
                    new List<string> { $"Allowed statuses: {string.Join(", ", ReportStatuses.All)}" });
            }

            var (items, totalCount) = await _reportRepository.GetPagedReviewReportsAsync(filter);

            var responses = new List<ReviewReportResponse>(items.Count);
            foreach (var report in items)
            {
                var review = report.TargetId.HasValue
                    ? await _reviewRepository.GetByIdAsync(report.TargetId.Value)
                    : null;
                responses.Add(report.ToReviewReportResponse(review));
            }

            var paged = new PagedResult<ReviewReportResponse>(
                responses, totalCount, filter.PageNumber, filter.PageSize);

            return Result<PagedResult<ReviewReportResponse>>.Success(paged);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Admin: resolve a report
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<ReviewReportResponse>> ResolveAsync(
            Guid adminId,
            Guid reportId,
            ResolveReviewReportRequest request)
        {
            var validationResult = await _resolveValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    validationResult.Errors.Select(x => x.ErrorMessage).ToList());
            }

            var report = await _reportRepository.GetByIdForUpdateAsync(reportId);
            if (report == null)
            {
                throw new NotFoundException(ErrorCodes.ReviewReportNotFound, "Review report not found");
            }

            if (report.Status is ReportStatuses.Resolved or ReportStatuses.Rejected)
            {
                throw new ConflictException(
                    ErrorCodes.ReviewReportNotAllowed,
                    $"Report is already {report.Status}");
            }

            var review = report.TargetId.HasValue
                ? await _reviewRepository.GetByIdForUpdateAsync(report.TargetId.Value)
                : null;

            Guid? coachToRecalculate = null;

            if (request.IsValid)
            {
                report.Status = ReportStatuses.Resolved;

                if (request.HideOrDeleteReview && review != null && review.Status == ReviewStatuses.Active)
                {
                    review.Status = ReviewStatuses.Hidden;
                    review.ModerationReason = string.IsNullOrWhiteSpace(request.ResolutionNote)
                        ? report.Reason
                        : request.ResolutionNote.Trim();
                    review.DeletedAt = DateTime.UtcNow;
                    review.DeletedByUserId = adminId;
                    review.UpdatedAt = DateTime.UtcNow;

                    report.ActionTaken = ReportActions.ReviewHidden;
                    coachToRecalculate = review.CoachId;
                }
                else
                {
                    report.ActionTaken = ReportActions.None;
                }
            }
            else
            {
                // Report rejected — the review stays active.
                report.Status = ReportStatuses.Rejected;
                report.ActionTaken = ReportActions.None;
            }

            report.HandledByUserId = adminId;
            report.HandledAt = DateTime.UtcNow;
            report.ResolutionNote = request.ResolutionNote?.Trim();

            // report and review are tracked on the same scoped DbContext → one save persists both.
            await _reportRepository.SaveChangesAsync();

            if (coachToRecalculate.HasValue)
            {
                await _reviewRepository.RecalculateCoachRatingAsync(coachToRecalculate.Value);
            }

            return Result<ReviewReportResponse>.Success(report.ToReviewReportResponse(review));
        }
    }
}
