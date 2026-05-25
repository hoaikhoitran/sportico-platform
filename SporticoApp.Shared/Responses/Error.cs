using SporticoApp.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Shared.Responses
{
    public class Error
    {
        public string Code { get; init; } = default!;

        public string Message { get; init; } = default!;

        public ErrorType Type { get; init; }

        public List<string>? Details { get; init; }
    }
}
