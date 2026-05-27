using FluentValidation;
using SporticoApp.Application.DTOs.Assessments;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Mappings;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Services
{
    using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

    public class LearnerAssessmentService : ILearnerAssessmentService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ILearnerAssessmentRepository _assessmentRepository;
        private readonly IValidator<CreateLearnerAssessmentRequest> _createValidator;
        private readonly IValidator<UpdateLearnerAssessmentRequest> _updateValidator;

        public LearnerAssessmentService(
            IBookingRepository bookingRepository,
            ILearnerAssessmentRepository assessmentRepository,
            IValidator<CreateLearnerAssessmentRequest> createValidator,
            IValidator<UpdateLearnerAssessmentRequest> updateValidator)
        {
            _bookingRepository = bookingRepository;
            _assessmentRepository = assessmentRepository;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<Result<LearnerAssessmentResponse>> CreateAsync(
            Guid learnerId,
            Guid bookingId,
            CreateLearnerAssessmentRequest request)
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

            var existingAssessment = await _assessmentRepository.GetByBookingIdAsync(bookingId);
            if (existingAssessment != null)
            {
                throw new ConflictException(
                    ErrorCodes.ValidationError,
                    "Assessment already exists");
            }

            var assessment = request.ToEntity(bookingId, booking.LearnerId, booking.CoachId);

            await _assessmentRepository.AddAsync(assessment);

            return Result<LearnerAssessmentResponse>.Success(assessment.ToResponse());
        }

        public async Task<Result<LearnerAssessmentResponse>> GetAsync(
            Guid userId,
            Guid bookingId)
        {
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

            var assessment = await _assessmentRepository.GetByBookingIdAsync(bookingId);
            if (assessment == null)
            {
                throw new NotFoundException(
                    ErrorCodes.LearnerAssessmentNotFound,
                    "Learner assessment not found");
            }

            return Result<LearnerAssessmentResponse>.Success(assessment.ToResponse());
        }

        public async Task<Result<LearnerAssessmentResponse>> UpdateAsync(
            Guid learnerId,
            Guid bookingId,
            UpdateLearnerAssessmentRequest request)
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

            var assessment = await _assessmentRepository.GetByBookingIdForUpdateAsync(bookingId);
            if (assessment == null)
            {
                throw new NotFoundException(
                    ErrorCodes.LearnerAssessmentNotFound,
                    "Learner assessment not found");
            }

            assessment.ApplyUpdate(request);

            await _assessmentRepository.SaveChangesAsync();

            return Result<LearnerAssessmentResponse>.Success(assessment.ToResponse());
        }
    }
}
