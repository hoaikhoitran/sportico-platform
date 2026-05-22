using SporticoApp.Application.DTOs.Auth;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Core.Entities;
using SporticoApp.Core.Enums;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Helpers;
using SporticoApp.Shared.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly IRoleRepository _roleRepo;
        private readonly IUserRoleRepository _userRoleRepo;
        private readonly IJwtService _jwtService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IEmailService _emailService;
        public AuthService(
            IUserRepository userRepo,
            IRoleRepository roleRepo,
            IUserRoleRepository userRoleRepo,
            IJwtService jwtService,
            IRefreshTokenService refreshTokenService,
            IEmailService emailService)
        {
            _userRepo = userRepo;
            _roleRepo = roleRepo;
            _userRoleRepo = userRoleRepo;
            _jwtService = jwtService;
            _refreshTokenService = refreshTokenService;
            _emailService = emailService;
        }

        public async Task<Result<LoginResponse>> LoginAsync(
                    LoginRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLower();
            var user = await _userRepo.GetByEmailAsync(normalizedEmail);
            if (user == null || !PasswordHelper.VerifyPassword(
                request.Password,
                user.PasswordHash))
            {
                return Result<LoginResponse>
                    .Fail("Invalid email or password");
            }
            else if(user.Status != UserStatus.active.ToString())
            {
                return Result<LoginResponse>
                    .Fail("Account is not active, check your email to active your account.");
            }

            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _refreshTokenService.GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiresAt = DateTime.UtcNow.Add(
                _refreshTokenService.GetRefreshTokenLifetime());

            await _userRepo.UpdateAsync(user);
            var response = new LoginResponse()
            {
                AccessToken = accessToken.Token,
                RefreshToken = refreshToken,
                ExpiresAt = accessToken.ExpiresAt
            };

            return Result<LoginResponse>.Success(response);

        }

        public async Task<Result> RegisterAsync(RegisterRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLower();
            var existingUser = await _userRepo.GetByEmailAsync(normalizedEmail);
            if (existingUser != null)
            {
                return Result.Fail("Email is already registered");
            }
            var verifyToken = Guid.NewGuid().ToString();

            var user = new User()
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                PasswordHash = PasswordHelper.HashPassword(request.Password),
                Status = UserStatus.inactive.ToString(),
                FullName = request.FullName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                EmailVerificationToken = verifyToken
            };

            await _userRepo.AddAsync(user);

            var verifyLink =
                $"https://localhost:7097/api/auth/verify-email?token={verifyToken}";
            await _emailService.SendEmailAsync(
            user.Email,
                "Verify your Sportico account",
                $@"
                    <h2>Welcome to Sportico</h2>

                    <p>Please click below to verify your account:</p>

                    <a href='{verifyLink}'>
                        Verify Email
                    </a>
                ");

            var leanerRole = await _roleRepo.GetByNameAsync(RoleConstants.Learner);
            if (leanerRole == null)
            {
                return Result.Fail("Learner role not found");
            }

            var userRole = new UserRole()
            {
                UserId = user.Id,
                RoleId = leanerRole.Id
            };

            await _userRoleRepo.AddAsync(userRole);
            return Result.Success("Registration successful");
        }

        public async Task<Result> VerifyEmailAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return Result.Fail("Invalid verification token");
            }

            var user = await _userRepo.GetByVerificationTokenAsync(token);
            if (user == null)
            {
                return Result.Fail("Invalid verification token");
            }

            user.Status = UserStatus.active.ToString();
            user.EmailVerificationToken = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepo.UpdateAsync(user);

            return Result.Success("Email verified successfully");
        }

        public async Task<Result<RefreshTokenResponse>> RefreshTokenAsync(
            RefreshTokenRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLower();
            var user = await _userRepo.GetByEmailAsync(normalizedEmail);
            if (user == null || user.RefreshToken != request.RefreshToken)
            {
                return Result<RefreshTokenResponse>.Fail("Invalid refresh token");
            }

            if (user.RefreshTokenExpiresAt == null ||
                user.RefreshTokenExpiresAt <= DateTime.UtcNow)
            {
                return Result<RefreshTokenResponse>.Fail("Refresh token expired");
            }

            var accessToken = _jwtService.GenerateAccessToken(user);
            var newRefreshToken = _refreshTokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiresAt = DateTime.UtcNow.Add(
                _refreshTokenService.GetRefreshTokenLifetime());

            await _userRepo.UpdateAsync(user);

            var response = new RefreshTokenResponse()
            {
                AccessToken = accessToken.Token,
                RefreshToken = newRefreshToken,
                ExpiresAt = accessToken.ExpiresAt
            };

            return Result<RefreshTokenResponse>.Success(response);
        }

    }
}
