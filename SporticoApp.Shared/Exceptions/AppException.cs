using SporticoApp.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Shared.Exceptions
{
    public class AppException : Exception
    {
        public string Code { get; }

        public ErrorType Type { get; }

        public List<string>? Details { get; }

        public AppException(
            string code,
            string message,
            ErrorType type,
            List<string>? details = null)
            : base(message)
        {
            Code = code;
            Type = type;
            Details = details;
        }
    }
}
