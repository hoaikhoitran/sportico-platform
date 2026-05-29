using SporticoApp.Application.DTOs.Auth;
using SporticoApp.Shared.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<Result<LoginResponse>> LoginAsync(LoginRequest request);
        Task<Result> RegisterAsync(RegisterRequest request);
        Task<Result> VerifyEmailAsync(string token);
        Task<Result<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest request);
        Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<Result> ResetPasswordAsync(ResetPasswordRequest request);
        Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
        Task<Result> ResendVerificationEmailAsync(ResendVerificationEmailRequest request);
    }
}
