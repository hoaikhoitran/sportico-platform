using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SporticoApp.Api.Configuration;
using SporticoApp.Application.DTOs.Auth;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Options;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using SporticoApp.Shared.Responses;
using System.Security.Claims;

namespace SporticoApp.Api.Controllers
{
    /// <summary>
    /// Google sign-in. Two flows share one account-resolution path:
    /// <list type="bullet">
    ///   <item><b>A — ID token:</b> POST /api/auth/google with a Google Identity Services credential.</item>
    ///   <item><b>B — redirect:</b> GET /api/auth/google → Google → callback → complete →
    ///         frontend receives a one-time code → POST /api/auth/google/exchange.</item>
    /// </list>
    /// Access and refresh tokens are NEVER placed in a redirect URL; only the single-use exchange
    /// code travels that way.
    /// </summary>
    [ApiController]
    [Route("api/auth/google")]
    [AllowAnonymous]
    public class GoogleAuthController : ControllerBase
    {
        private readonly IGoogleAuthService _googleAuthService;
        private readonly GoogleAuthOptions _options;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<GoogleAuthController> _logger;

        public GoogleAuthController(
            IGoogleAuthService googleAuthService,
            IOptions<GoogleAuthOptions> options,
            IWebHostEnvironment environment,
            ILogger<GoogleAuthController> logger)
        {
            _googleAuthService = googleAuthService;
            _options = options.Value;
            _environment = environment;
            _logger = logger;
        }

        // ── Flow A ──────────────────────────────────────────────────────────────

        /// <summary>Exchanges a verified Google ID token for Sportico tokens.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(Result<LoginResponse>), 200)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 503)]
        public async Task<IActionResult> LoginWithIdToken([FromBody] GoogleIdTokenLoginRequest request)
        {
            var result = await _googleAuthService.LoginWithIdTokenAsync(request);
            return Ok(result);
        }

        // ── Flow B ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Starts the browser redirect flow. This is a navigation endpoint: it answers 302 to
        /// Google, never JSON. Do not call it with fetch/XHR.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(302)]
        [ProducesResponseType(typeof(Result<object>), 503)]
        public IActionResult StartRedirect()
        {
            EnsureRedirectFlowConfigured();

            // The client cannot influence where we end up: RedirectUri is a fixed local path, and
            // the final hop is always built from the configured FRONTEND_URL.
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(Complete)) ?? "/api/auth/google/complete",
                IsPersistent = false
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Runs after the Google handler has validated the callback and written the temporary
        /// external cookie. Resolves the Sportico account, mints a one-time exchange code, drops
        /// the temporary cookie, and redirects to the frontend.
        /// </summary>
        [HttpGet("complete")]
        [ProducesResponseType(302)]
        public async Task<IActionResult> Complete()
        {
            EnsureRedirectFlowConfigured();

            var authenticateResult = await HttpContext.AuthenticateAsync(
                AuthenticationSchemeNames.ExternalCookie);

            if (!authenticateResult.Succeeded || authenticateResult.Principal == null)
            {
                return await FailAsync(ErrorCodes.GoogleExternalPrincipalInvalid);
            }

            var identity = MapPrincipal(authenticateResult.Principal);
            if (identity == null)
            {
                return await FailAsync(ErrorCodes.GoogleExternalPrincipalInvalid);
            }

            string code;
            try
            {
                code = await _googleAuthService.CreateExchangeCodeForIdentityAsync(identity);
            }
            catch (AppException ex)
            {
                // Surface a STABLE error code only. An exception message could contain account
                // details, and would end up in the user's browser history.
                _logger.LogWarning("Google redirect login rejected: {Code}", ex.Code);
                return await FailAsync(ex.Code);
            }
            catch (Exception ex)
            {
                _logger.LogError("Google redirect login failed: {ExceptionType}", ex.GetType().FullName);
                return await FailAsync(ErrorCodes.GoogleLoginFailed);
            }

            // The temporary external cookie has done its job; do not leave it on the browser.
            await HttpContext.SignOutAsync(AuthenticationSchemeNames.ExternalCookie);

            return Redirect(GoogleCallbackUrlResolver.BuildFrontendCallbackUrl(_options, code, null));
        }

        /// <summary>Trades the one-time code for Sportico tokens.</summary>
        [HttpPost("exchange")]
        [ProducesResponseType(typeof(Result<LoginResponse>), 200)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 409)]
        public async Task<IActionResult> Exchange([FromBody] GoogleExchangeCodeRequest request)
        {
            var result = await _googleAuthService.ExchangeCodeAsync(request);
            return Ok(result);
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private async Task<IActionResult> FailAsync(string errorCode)
        {
            await HttpContext.SignOutAsync(AuthenticationSchemeNames.ExternalCookie);
            return Redirect(GoogleCallbackUrlResolver.BuildFrontendCallbackUrl(_options, null, errorCode));
        }

        private void EnsureRedirectFlowConfigured()
        {
            if (_options.IsRedirectFlowConfigured &&
                GoogleCallbackUrlResolver.IsCallbackUrlValid(_options, _environment.IsDevelopment()))
            {
                return;
            }

            var missing = _options.MissingRedirectFlowKeys();
            if (missing.Count == 0)
            {
                // Everything is present but the callback URL itself is unusable.
                missing.Add("GOOGLE_CALLBACK_URL");
            }

            // Key NAMES only — never values.
            throw new ServiceUnavailableException(
                ErrorCodes.GoogleConfigurationMissing,
                "Google sign-in is not configured on this environment.",
                missing);
        }

        /// <summary>
        /// Projects the temporary external principal onto the same verified-identity shape the
        /// ID-token flow produces. The Google handler has already validated the authorization code
        /// against Google, so these claims are trustworthy.
        /// </summary>
        private static GoogleIdentity? MapPrincipal(ClaimsPrincipal principal)
        {
            var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = principal.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            return new GoogleIdentity
            {
                Subject = subject,
                Email = email,
                // Google only returns a profile through this flow after the user authenticated with
                // Google itself; the email_verified claim is surfaced by the handler when present.
                EmailVerified = ReadEmailVerified(principal),
                FullName = principal.FindFirstValue(ClaimTypes.Name),
                AvatarUrl = principal.FindFirstValue("urn:google:picture")
            };
        }

        private static bool ReadEmailVerified(ClaimsPrincipal principal)
        {
            var raw = principal.FindFirstValue("email_verified")
                ?? principal.FindFirstValue("urn:google:email_verified");

            // Absent claim: the handler completed a real Google sign-in for this address, and Google
            // does not return unverified addresses through the OAuth userinfo endpoint.
            if (string.IsNullOrWhiteSpace(raw))
            {
                return true;
            }

            return bool.TryParse(raw, out var parsed) && parsed;
        }
    }
}
