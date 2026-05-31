using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.Reviews;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        // ── Public ────────────────────────────────────────────────────────────

        [HttpGet("api/coaches/{coachId:guid}/reviews")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<PagedResult<ReviewResponse>>), 200)]
        public async Task<IActionResult> GetCoachReviews(
            Guid coachId,
            [FromQuery] ReviewFilterRequest filter)
        {
            var currentUserId = User.GetUserIdOrNull();
            var result = await _reviewService.GetCoachReviewsAsync(currentUserId, coachId, filter);
            return Ok(result);
        }

        [HttpGet("api/coaches/{coachId:guid}/reviews/summary")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<CoachReviewSummaryResponse>), 200)]
        public async Task<IActionResult> GetCoachReviewSummary(Guid coachId)
        {
            var result = await _reviewService.GetCoachReviewSummaryAsync(coachId);
            return Ok(result);
        }

        // ── Learner ───────────────────────────────────────────────────────────

        [HttpGet("api/coaches/{coachId:guid}/reviews/me")]
        [Authorize(Roles = RoleConstants.Learner)]
        [ProducesResponseType(typeof(Result<ReviewResponse>), 200)]
        public async Task<IActionResult> GetMyReviewForCoach(Guid coachId)
        {
            var learnerId = User.GetUserId();
            var result = await _reviewService.GetMyReviewForCoachAsync(learnerId, coachId);
            return Ok(result);
        }

        [HttpPost("api/coaches/{coachId:guid}/reviews")]
        [Authorize(Roles = RoleConstants.Learner)]
        [ProducesResponseType(typeof(Result<ReviewResponse>), 200)]
        public async Task<IActionResult> CreateReview(
            Guid coachId,
            [FromBody] CreateReviewRequest request)
        {
            var learnerId = User.GetUserId();
            // The route coachId is authoritative.
            request.CoachId = coachId;
            var result = await _reviewService.CreateAsync(learnerId, request);
            return Ok(result);
        }

        [HttpPut("api/reviews/{id:guid}")]
        [Authorize(Roles = RoleConstants.Learner)]
        [ProducesResponseType(typeof(Result<ReviewResponse>), 200)]
        public async Task<IActionResult> UpdateReview(
            Guid id,
            [FromBody] UpdateReviewRequest request)
        {
            var learnerId = User.GetUserId();
            var result = await _reviewService.UpdateAsync(learnerId, id, request);
            return Ok(result);
        }

        [HttpDelete("api/reviews/{id:guid}")]
        [Authorize(Roles = RoleConstants.Learner)]
        [ProducesResponseType(typeof(Result<object>), 200)]
        public async Task<IActionResult> DeleteReview(Guid id)
        {
            var learnerId = User.GetUserId();
            var result = await _reviewService.DeleteOwnAsync(learnerId, id);
            return Ok(result);
        }
    }
}
