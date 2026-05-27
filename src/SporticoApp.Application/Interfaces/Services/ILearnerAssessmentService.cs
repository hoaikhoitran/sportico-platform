using SporticoApp.Application.DTOs.Assessments;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface ILearnerAssessmentService
    {
        Task<Result<LearnerAssessmentResponse>> CreateAsync(
            Guid learnerId,
            Guid bookingId,
            CreateLearnerAssessmentRequest request);

        Task<Result<LearnerAssessmentResponse>> GetAsync(
            Guid userId,
            Guid bookingId);

        Task<Result<LearnerAssessmentResponse>> UpdateAsync(
            Guid learnerId,
            Guid bookingId,
            UpdateLearnerAssessmentRequest request);
    }
}
