using System;

namespace SporticoApp.Application.Validators
{
    /// <summary>
    /// Shared predicate helpers used across FluentValidation validators.
    /// </summary>
    public static class ValidationRules
    {
        /// <summary>
        /// True when the value is null/empty (handled by other rules) or a valid
        /// absolute http/https URL.
        /// </summary>
        public static bool BeAValidAbsoluteUrlOrEmpty(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            return Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// True when the value is a valid absolute http/https URL. Use for required URLs.
        /// </summary>
        public static bool BeAValidAbsoluteUrl(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}
