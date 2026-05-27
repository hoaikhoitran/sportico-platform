using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.Bookings;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Route("api/bookings")]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpPost("purchase/manual")]
        [Authorize(Roles = RoleConstants.Learner)]
        [ProducesResponseType(typeof(Result<BookingResponse>), 200)]
        public async Task<IActionResult> PurchaseManual(
            [FromBody] PurchaseTrainingPackageManualRequest request)
        {
            var learnerId = User.GetUserId();
            var result = await _bookingService.PurchaseManualAsync(learnerId, request);
            return Ok(result);
        }

        [HttpPost("purchase/payos")]
        [Authorize(Roles = RoleConstants.Learner)]
        [ProducesResponseType(typeof(Result<PurchaseTrainingPackagePayOsResponse>), 200)]
        public async Task<IActionResult> PurchasePayOs(
            [FromBody] PurchaseTrainingPackagePayOsRequest request)
        {
            var learnerId = User.GetUserId();
            var result = await _bookingService.PurchaseWithPayOsAsync(learnerId, request);
            return Ok(result);
        }

        [HttpGet("me")]
        [Authorize(Roles = RoleConstants.Learner)]
        [ProducesResponseType(typeof(Result<PagedResult<BookingResponse>>), 200)]
        public async Task<IActionResult> GetMyBookings([FromQuery] BookingFilterRequest filter)
        {
            var learnerId = User.GetUserId();
            var result = await _bookingService.GetMyBookingsAsync(learnerId, filter);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        [Authorize(Roles = RoleConstants.Learner)]
        [ProducesResponseType(typeof(Result<BookingResponse>), 200)]
        public async Task<IActionResult> GetMyBookingById(Guid id)
        {
            var learnerId = User.GetUserId();
            var result = await _bookingService.GetMyBookingByIdAsync(learnerId, id);
            return Ok(result);
        }

        [HttpGet("coach")]
        [Authorize(Roles = RoleConstants.Coach)]
        [ProducesResponseType(typeof(Result<PagedResult<BookingResponse>>), 200)]
        public async Task<IActionResult> GetCoachBookings([FromQuery] BookingFilterRequest filter)
        {
            var coachId = User.GetUserId();
            var result = await _bookingService.GetCoachBookingsAsync(coachId, filter);
            return Ok(result);
        }

        [HttpGet("coach/{id:guid}")]
        [Authorize(Roles = RoleConstants.Coach)]
        [ProducesResponseType(typeof(Result<BookingResponse>), 200)]
        public async Task<IActionResult> GetCoachBookingById(Guid id)
        {
            var coachId = User.GetUserId();
            var result = await _bookingService.GetCoachBookingByIdAsync(coachId, id);
            return Ok(result);
        }
    }
}
