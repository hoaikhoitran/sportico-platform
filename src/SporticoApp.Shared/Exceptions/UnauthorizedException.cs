using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Shared.Exceptions
{
    public class UnauthorizedException : AppException
    {
        public UnauthorizedException(
            string code,
            string message)
            : base(
                code,
                message,
                ErrorType.Unauthorized)
        {
        }
    }
}
