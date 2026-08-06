using SporticoApp.Shared.Enums;

namespace SporticoApp.Shared.Exceptions
{
    /// <summary>
    /// A dependency the endpoint needs is missing or temporarily unavailable — for example the
    /// Google OAuth credentials are not configured on this environment. Maps to HTTP 503.
    /// The message must never contain a secret value; name the missing configuration KEY only.
    /// </summary>
    public class ServiceUnavailableException : AppException
    {
        public ServiceUnavailableException(
            string code,
            string message,
            List<string>? details = null)
            : base(
                code,
                message,
                ErrorType.ServiceUnavailable,
                details)
        {
        }
    }
}
