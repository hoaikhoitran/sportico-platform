using SporticoApp.Application.DTOs.Auth;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Core.Entities;
using SporticoApp.Core.Enums;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using SporticoApp.Shared.Helpers;
using SporticoApp.Shared.Responses;
using Microsoft.Extensions.Configuration;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Application.Services
{
    using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly IRoleRepository _roleRepo;
        private readonly IUserRoleRepository _userRoleRepo;
        private readonly IJwtService _jwtService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IValidator<RefreshTokenRequest> _refreshTokenValidator;
        public AuthService(
            IUserRepository userRepo,
            IRoleRepository roleRepo,
            IUserRoleRepository userRoleRepo,
            IJwtService jwtService,
            IRefreshTokenService refreshTokenService,
            IEmailService emailService,
            IConfiguration configuration,
            IValidator<RefreshTokenRequest> refreshTokenValidator)
        {
            _userRepo = userRepo;
            _roleRepo = roleRepo;
            _userRoleRepo = userRoleRepo;
            _jwtService = jwtService;
            _refreshTokenService = refreshTokenService;
            _emailService = emailService;
            _configuration = configuration;
            _refreshTokenValidator = refreshTokenValidator;
        }

        public async Task<Result<LoginResponse>> LoginAsync(
                    LoginRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLower();
            var user = await _userRepo.GetByEmailWithRolesAsync(normalizedEmail);
            if (user == null || !PasswordHelper.VerifyPassword(
                request.Password,
                user.PasswordHash))
            {
                throw new UnauthorizedException(ErrorCodes.InvalidCredentials, "Invalid Email or Password");
            }
            else if(user.Status != UserStatus.active.ToString())
            {
                throw new UnauthorizedException(ErrorCodes.AccountNotActive, "Account is not active, check your email to active your account.");
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
                throw new ConflictException(ErrorCodes.EmailAlreadyExists, "Email is already registered");
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

            var apiBaseUrl = _configuration["AppSettings:ApiBaseUrl"];

            if (string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                throw new InvalidOperationException("AppSettings:ApiBaseUrl is missing.");
            }

            var verifyLink =
                $"{apiBaseUrl.TrimEnd('/')}/api/auth/verify-email?token={verifyToken}";
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
                throw new NotFoundException(
                    ErrorCodes.RoleNotFound,
                    "Learner role not found");
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
                throw new ValidationException(ErrorCodes.InvalidVerificationToken, "Verification token is required");
            }

            var user = await _userRepo.GetByVerificationTokenAsync(token);
            if (user == null)
            {
                throw new ValidationException(ErrorCodes.InvalidVerificationToken, "Invalid verification token");
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
            var validationResult = await _refreshTokenValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors
                    .Select(x => x.ErrorMessage)
                    .ToList();

                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    details);
            }

            var normalizedEmail = request.Email.Trim().ToLower();
            var user = await _userRepo.GetByEmailWithRolesAsync(normalizedEmail);
            if (user == null || user.RefreshToken != request.RefreshToken)
            {
                throw new UnauthorizedException(ErrorCodes.InvalidRefreshToken, "Invalid refresh token");
            }

            if (user.Status != UserStatus.active.ToString())
            {
                throw new UnauthorizedException(
                    ErrorCodes.AccountNotActive,
                    "Account is not active, check your email to active your account.");
            }

            if (user.RefreshTokenExpiresAt == null ||
                user.RefreshTokenExpiresAt <= DateTime.UtcNow)
            {
                throw new UnauthorizedException(ErrorCodes.RefreshTokenExpired, "Refresh token expired");
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
