using SporticoApp.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Shared.Exceptions
{
    public class FailureException : AppException
    {
        public FailureException(
            string code,
            string message,
            List<string>? details = null)
            : base(code, message, ErrorType.Failure, details)
        {
        }
    }
}
