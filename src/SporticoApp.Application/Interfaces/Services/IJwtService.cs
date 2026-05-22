using SporticoApp.Application.DTOs.Auth;
using SporticoApp.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IJwtService
    {
        TokenResult GenerateAccessToken(User user);
    }
}
