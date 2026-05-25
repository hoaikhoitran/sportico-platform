using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Shared.Exceptions
{
    public class ForbiddenException : AppException
    {
        public ForbiddenException(
            string code,
            string message)
            : base(
                code,
                message,
                ErrorType.Forbidden)
        {
        }
    }
}
