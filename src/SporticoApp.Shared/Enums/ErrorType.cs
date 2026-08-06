using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Shared.Enums
{
    public enum ErrorType
    {
        Validation,
        NotFound,
        Unauthorized,
        Forbidden,
        Conflict,
        Failure,

        /// <summary>
        /// A dependency the endpoint needs is not configured or is temporarily unavailable
        /// (e.g. Google OAuth credentials are absent). Maps to HTTP 503.
        /// Appended last on purpose so every existing member keeps its numeric value.
        /// </summary>
        ServiceUnavailable
    }
}
