using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SporticoApp.Api.Extensions;
using SporticoApp.Api.Middlewares;
using SporticoApp.Application.DTOs.Analytics;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;
using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

namespace SporticoApp.Api.Controllers
{
    /// <summary>
    /// Public, intentionally unauthenticated ingestion endpoint for FRONTEND navigation events. No
    /// [Authorize] — it must work for anonymous visitors; User.GetUserIdOrNull() still attributes the
    /// event to a logged-in user when a valid token is present. This is the only source of PageView
    /// rows — the backend never infers a frontend route from a backend API path (see PageView entity).
    /// </summary>
    [ApiController]
    [Route("api/analytics")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IVisitorTrackingQueue _queue;
        private readonly IValidator<SubmitPageViewRequest> _validator;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(
            IVisitorTrackingQueue queue,
            IValidator<SubmitPageViewRequest> validator,
            ILogger<AnalyticsController> logger)
        {
            _queue = queue;
            _validator = validator;
            _logger = logger;
        }

        [HttpPost("pageview")]
        [ProducesResponseType(typeof(Result), 202)]
        public async Task<IActionResult> SubmitPageView([FromBody] SubmitPageViewRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                throw new ValidationException(ErrorCodes.ValidationError, "Invalid request data", details);
            }

            // Resolved by VisitorTrackingMiddleware earlier in this same request's pipeline. Absent
            // only when analytics is globally disabled or this request was classified as a bot — in
            // either case, accept the submission as a no-op rather than erroring the caller.
            //
            // Enqueueing is wrapped so a tracking-side fault can never turn this otherwise-successful
            // 202 into a 500 — analytics is best-effort and must never affect the caller's outcome.
            if (HttpContext.Items[VisitorTrackingMiddleware.VisitorIdItemKey] is Guid visitorId)
            {
                try
                {
                    _queue.TryEnqueue(new VisitorTrackingWorkItem
                    {
                        Kind = VisitorTrackingWorkItemKind.PageView,
                        Context = new VisitContext
                        {
                            VisitorId = visitorId,
                            IpAddress = VisitorSignalResolver.ResolveClientIp(HttpContext),
                            UserAgent = VisitorSignalResolver.ResolveUserAgent(HttpContext),
                            Country = VisitorSignalResolver.ResolveCountry(HttpContext),
                            UserId = User.GetUserIdOrNull()
                        },
                        Path = request.Path,
                        Title = request.Title,
                        Referrer = request.Referrer
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Page-view enqueue failed for path {Path}; submission still accepted.", request.Path);
                }
            }

            return Accepted(Result.Success());
        }
    }
}
