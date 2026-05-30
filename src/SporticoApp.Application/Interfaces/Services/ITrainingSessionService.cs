using SporticoApp.Application.DTOs.TrainingSessions;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface ITrainingSessionService
    {
        Task<Result<TrainingSessionResponse>> CreateAsync(
            Guid learnerId,
            CreateTrainingSessionRequest request);

        Task<Result<PagedResult<TrainingSessionResponse>>> GetByBookingAsync(
            Guid userId,
            Guid bookingId,
            TrainingSessionFilterRequest filter);

        Task<Result<TrainingSessionResponse>> ConfirmAsync(
            Guid coachId,
            Guid sessionId,
            ConfirmTrainingSessionRequest request);

        Task<Result<TrainingSessionResponse>> CancelAsync(
            Guid userId,
            Guid sessionId,
            CancelTrainingSessionRequest request);

        Task<Result<TrainingSessionResponse>> CompleteAsync(
            Guid coachId,
            Guid sessionId);

        Task<Result<PagedResult<TrainingSessionResponse>>> GetMySessionsAsLearnerAsync(
            Guid learnerId,
            TrainingSessionFilterRequest filter);

        Task<Result<PagedResult<TrainingSessionResponse>>> GetMySessionsAsCoachAsync(
            Guid coachId,
            TrainingSessionFilterRequest filter);
    }
}
