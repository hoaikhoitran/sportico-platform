using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Application.DTOs.Payments;
using SporticoApp.Application.Interfaces.Services;
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

        [HttpPost("payos/webhook")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<object>), 200)]
        public async Task<IActionResult> PayOsWebhook([FromBody] PayOsWebhookRequest request)
        {
            var result = await _bookingService.HandlePayOsWebhookAsync(request);
            return Ok(result);
        }
    }
}
