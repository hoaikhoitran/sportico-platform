using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Application.DTOs.Auth
{
    public class LoginResponse
    {
        public string AccessToken { get; set; } = default;
        public string RefreshToken { get; set; } = default;
    }
}
