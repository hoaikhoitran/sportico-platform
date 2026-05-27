using FluentValidation;
using SporticoApp.Application.DTOs.ProgressCheckIns;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Mappings;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Services
{
    using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

    public class ProgressCheckInService : IProgressCheckInService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IProgressCheckInRepository _progressCheckInRepository;
        private readonly IValidator<CreateProgressCheckInRequest> _createValidator;
        private readonly IValidator<ProgressCheckInFilterRequest> _filterValidator;
        private readonly IValidator<UpdateProgressCheckInFeedbackRequest> _feedbackValidator;

        public ProgressCheckInService(
            IBookingRepository bookingRepository,
            IProgressCheckInRepository progressCheckInRepository,
            IValidator<CreateProgressCheckInRequest> createValidator,
            IValidator<ProgressCheckInFilterRequest> filterValidator,
            IValidator<UpdateProgressCheckInFeedbackRequest> feedbackValidator)
        {
            _bookingRepository = bookingRepository;
            _progressCheckInRepository = progressCheckInRepository;
            _createValidator = createValidator;
            _filterValidator = filterValidator;
            _feedbackValidator = feedbackValidator;
        }

        public async Task<Result<ProgressCheckInResponse>> CreateAsync(
            Guid learnerId,
            Guid bookingId,
            CreateProgressCheckInRequest request)
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

            var booking = await _bookingRepository.GetByIdForLearnerAsync(learnerId, bookingId);
            if (booking == null)
            {
                var existing = await _bookingRepository.GetByIdAsync(bookingId);
                if (existing != null)
                {
                    throw new ForbiddenException(
                        ErrorCodes.BookingNotOwned,
                        "Booking is not owned by the current learner");
                }

                throw new NotFoundException(
                    ErrorCodes.BookingNotFound,
                    "Booking not found");
            }

            if (booking.Status != BookingStatuses.Active)
            {
                throw new ConflictException(
                    ErrorCodes.BookingNotActive,
                    "Booking is not active");
            }

            var checkIn = request.ToEntity(bookingId, booking.LearnerId, booking.CoachId);

            await _progressCheckInRepository.AddAsync(checkIn);

            return Result<ProgressCheckInResponse>.Success(checkIn.ToResponse());
        }

        public async Task<Result<PagedResult<ProgressCheckInResponse>>> GetByBookingAsync(
            Guid userId,
            Guid bookingId,
            ProgressCheckInFilterRequest filter)
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

            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null)
            {
                throw new NotFoundException(
                    ErrorCodes.BookingNotFound,
                    "Booking not found");
            }

            if (booking.LearnerId != userId && booking.CoachId != userId)
            {
                throw new ForbiddenException(
                    ErrorCodes.BookingNotOwned,
                    "Booking is not accessible by the current user");
            }

            var (items, totalCount) = await _progressCheckInRepository.GetByBookingPagedAsync(
                bookingId,
                filter.PageNumber,
                filter.PageSize);

            var response = new PagedResult<ProgressCheckInResponse>(
                items.Select(x => x.ToResponse()).ToList(),
                totalCount,
                filter.PageNumber,
                filter.PageSize);

            return Result<PagedResult<ProgressCheckInResponse>>.Success(response);
        }

        public async Task<Result<ProgressCheckInResponse>> UpdateFeedbackAsync(
            Guid coachId,
            Guid checkInId,
            UpdateProgressCheckInFeedbackRequest request)
        {
            var validationResult = await _feedbackValidator.ValidateAsync(request);
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

            var checkIn = await _progressCheckInRepository.GetByIdForUpdateAsync(checkInId);
            if (checkIn == null)
            {
                throw new NotFoundException(
                    ErrorCodes.ProgressCheckInNotFound,
                    "Progress check-in not found");
            }

            if (checkIn.CoachId != coachId)
            {
                throw new ForbiddenException(
                    ErrorCodes.BookingNotOwned,
                    "Progress check-in is not owned by the current coach");
            }

            checkIn.CoachFeedback = request.CoachFeedback.Trim();
            checkIn.UpdatedAt = DateTime.UtcNow;

            await _progressCheckInRepository.SaveChangesAsync();

            return Result<ProgressCheckInResponse>.Success(checkIn.ToResponse());
        }
    }
}
