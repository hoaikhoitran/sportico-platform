using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Application.DTOs.Payments;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Api.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public PaymentsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        // PayOS calls this server-to-server with a signed payload. It MUST stay anonymous;
        // the signature (not auth) is what proves authenticity.
        [HttpPost("payos/webhook")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<object>), 200)]
        public async Task<IActionResult> PayOsWebhook([FromBody] PayOsWebhookRequest request)
        {
            var result = await _bookingService.HandlePayOsWebhookAsync(request);
            return Ok(result);
        }

        // Learner-initiated reconcile after returning from PayOS. Authenticated; the service
        // verifies the payment belongs to the caller and checks the real state with PayOS.
        [HttpPost("payos/reconcile")]
        [Authorize(Roles = RoleConstants.Learner)]
        [ProducesResponseType(typeof(Result<ReconcilePayOsResponse>), 200)]
        public async Task<IActionResult> ReconcilePayOs([FromBody] ReconcilePayOsRequest request)
        {
            var learnerId = User.GetUserId();
            var result = await _bookingService.ReconcilePayOsAsync(learnerId, request);
            return Ok(result);
        }

        // Convenience route variant: orderCode in the path.
        [HttpPost("payos/{orderCode:long}/reconcile")]
        [Authorize(Roles = RoleConstants.Learner)]
        [ProducesResponseType(typeof(Result<ReconcilePayOsResponse>), 200)]
        public async Task<IActionResult> ReconcilePayOsByOrderCode(long orderCode)
        {
            var learnerId = User.GetUserId();
            var result = await _bookingService.ReconcilePayOsAsync(
                learnerId,
                new ReconcilePayOsRequest { OrderCode = orderCode });
            return Ok(result);
        }
    }
}
