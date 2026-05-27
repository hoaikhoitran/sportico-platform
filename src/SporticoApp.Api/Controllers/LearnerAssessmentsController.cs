using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.Assessments;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Authorize]
    public class LearnerAssessmentsController : ControllerBase
    {
        private readonly ILearnerAssessmentService _assessmentService;

        public LearnerAssessmentsController(ILearnerAssessmentService assessmentService)
        {
            _assessmentService = assessmentService;
        }

        [HttpPost("api/bookings/{bookingId:guid}/assessment")]
        [Authorize(Roles = RoleConstants.Learner)]
        [ProducesResponseType(typeof(Result<LearnerAssessmentResponse>), 200)]
        public async Task<IActionResult> Create(
            Guid bookingId,
            [FromBody] CreateLearnerAssessmentRequest request)
        {
            var learnerId = User.GetUserId();
            var result = await _assessmentService.CreateAsync(learnerId, bookingId, request);
            return Ok(result);
        }

        [HttpGet("api/bookings/{bookingId:guid}/assessment")]
        [Authorize]
        [ProducesResponseType(typeof(Result<LearnerAssessmentResponse>), 200)]
        public async Task<IActionResult> Get(Guid bookingId)
        {
            var userId = User.GetUserId();
            var result = await _assessmentService.GetAsync(userId, bookingId);
            return Ok(result);
        }

        [HttpPut("api/bookings/{bookingId:guid}/assessment")]
        [Authorize(Roles = RoleConstants.Learner)]
        [ProducesResponseType(typeof(Result<LearnerAssessmentResponse>), 200)]
        public async Task<IActionResult> Update(
            Guid bookingId,
            [FromBody] UpdateLearnerAssessmentRequest request)
        {
            var learnerId = User.GetUserId();
            var result = await _assessmentService.UpdateAsync(learnerId, bookingId, request);
            return Ok(result);
        }
    }
}
