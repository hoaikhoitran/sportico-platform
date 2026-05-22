using SporticoApp.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IRefreshTokenService
    {
        string GenerateRefreshToken();

        TimeSpan GetRefreshTokenLifetime();

    }
}
