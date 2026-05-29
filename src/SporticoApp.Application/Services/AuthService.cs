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
        private const string ForgotPasswordGenericMessage =
            "If the email exists, a password reset link has been sent.";

        private const string ResendVerificationGenericMessage =
            "If the account exists and is not verified, a verification email has been sent.";

        private static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromMinutes(30);

        private readonly IUserRepository _userRepo;
        private readonly IRoleRepository _roleRepo;
        private readonly IUserRoleRepository _userRoleRepo;
        private readonly IJwtService _jwtService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IEmailService _emailService;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IConfiguration _configuration;
        private readonly IValidator<RefreshTokenRequest> _refreshTokenValidator;
        private readonly IValidator<ForgotPasswordRequest> _forgotPasswordValidator;
        private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator;
        private readonly IValidator<ChangePasswordRequest> _changePasswordValidator;
        private readonly IValidator<ResendVerificationEmailRequest> _resendVerificationValidator;

        public AuthService(
            IUserRepository userRepo,
            IRoleRepository roleRepo,
            IUserRoleRepository userRoleRepo,
            IJwtService jwtService,
            IRefreshTokenService refreshTokenService,
            IEmailService emailService,
            IEmailTemplateService emailTemplateService,
            IConfiguration configuration,
            IValidator<RefreshTokenRequest> refreshTokenValidator,
            IValidator<ForgotPasswordRequest> forgotPasswordValidator,
            IValidator<ResetPasswordRequest> resetPasswordValidator,
            IValidator<ChangePasswordRequest> changePasswordValidator,
            IValidator<ResendVerificationEmailRequest> resendVerificationValidator)
        {
            _userRepo = userRepo;
            _roleRepo = roleRepo;
            _userRoleRepo = userRoleRepo;
            _jwtService = jwtService;
            _refreshTokenService = refreshTokenService;
            _emailService = emailService;
            _emailTemplateService = emailTemplateService;
            _configuration = configuration;
            _refreshTokenValidator = refreshTokenValidator;
            _forgotPasswordValidator = forgotPasswordValidator;
            _resetPasswordValidator = resetPasswordValidator;
            _changePasswordValidator = changePasswordValidator;
            _resendVerificationValidator = resendVerificationValidator;
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
            // 1. Normalize email.
            var normalizedEmail = request.Email.Trim().ToLower();

            // 2. Check for a duplicate before creating anything.
            var existingUser = await _userRepo.GetByEmailAsync(normalizedEmail);
            if (existingUser != null)
            {
                throw new ConflictException(ErrorCodes.EmailAlreadyExists, "Email is already registered");
            }

            // 3. Resolve the learner role BEFORE creating the user so we never
            //    persist a user without a role.
            var learnerRole = await _roleRepo.GetByNameAsync(RoleConstants.Learner);
            if (learnerRole == null)
            {
                throw new NotFoundException(
                    ErrorCodes.RoleNotFound,
                    "Learner role not found");
            }

            var verifyToken = SecureTokenGenerator.Generate();
            var now = DateTime.UtcNow;

            var user = new User()
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                PasswordHash = PasswordHelper.HashPassword(request.Password),
                Status = UserStatus.inactive.ToString(),
                FullName = request.FullName.Trim(),
                CreatedAt = now,
                UpdatedAt = now,
                EmailVerificationToken = verifyToken
            };

            // 4 + 5. Create user and userRole, then save together (shared DbContext).
            await _userRepo.AddWithoutSaveAsync(user);
            await _userRoleRepo.AddWithoutSaveAsync(new UserRole
            {
                UserId = user.Id,
                RoleId = learnerRole.Id,
                CreatedAt = now
            });

            await _userRepo.SaveChangesAsync();

            // 6. Send verification email after the database save. Email delivery is
            //    best-effort: if it fails the account still exists (inactive) and the
            //    user can request a new verification email via resend-verification-email.
            try
            {
                var verifyLink = BuildVerifyLink(verifyToken);
                var body = _emailTemplateService.BuildVerifyEmailTemplate(user.FullName, verifyLink);

                await _emailService.SendEmailAsync(
                    user.Email,
                    "Verify your Sportico account",
                    body);
            }
            catch
            {
                // Swallow: do not roll back the created user. The verification email
                // can be resent later.
            }

            return Result.Success("Registration successful. Please check your email to verify your account.");
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

        public async Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var validationResult = await _forgotPasswordValidator.ValidateAsync(request);
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
            var user = await _userRepo.GetByEmailAsync(normalizedEmail);

            // Only act for active accounts, but never reveal whether the email exists.
            if (user != null && user.Status == UserStatus.active.ToString())
            {
                var resetToken = SecureTokenGenerator.Generate();

                user.PasswordResetToken = resetToken;
                user.PasswordResetTokenExpiresAt = DateTime.UtcNow.Add(PasswordResetTokenLifetime);
                user.UpdatedAt = DateTime.UtcNow;

                await _userRepo.UpdateAsync(user);

                var resetLink = BuildResetLink(resetToken);
                var body = _emailTemplateService.BuildResetPasswordTemplate(user.FullName, resetLink);

                await _emailService.SendEmailAsync(
                    user.Email,
                    "Reset your Sportico password",
                    body);
            }

            return Result.Success(ForgotPasswordGenericMessage);
        }

        public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var validationResult = await _resetPasswordValidator.ValidateAsync(request);
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

            var user = await _userRepo.GetByPasswordResetTokenAsync(request.Token.Trim());
            if (user == null)
            {
                throw new ValidationException(
                    ErrorCodes.InvalidPasswordResetToken,
                    "Invalid password reset token");
            }

            if (user.PasswordResetTokenExpiresAt == null ||
                user.PasswordResetTokenExpiresAt <= DateTime.UtcNow)
            {
                throw new ValidationException(
                    ErrorCodes.PasswordResetTokenExpired,
                    "Password reset token has expired");
            }

            user.PasswordHash = PasswordHelper.HashPassword(request.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiresAt = null;

            // Invalidate any existing sessions for security.
            user.RefreshToken = null;
            user.RefreshTokenExpiresAt = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepo.UpdateAsync(user);

            return Result.Success("Password has been reset successfully. Please log in with your new password.");
        }

        public async Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            var validationResult = await _changePasswordValidator.ValidateAsync(request);
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

            var user = await _userRepo.GetByIdForUpdateAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(
                    ErrorCodes.UserNotFound,
                    "User not found");
            }

            if (!PasswordHelper.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                throw new UnauthorizedException(
                    ErrorCodes.InvalidCurrentPassword,
                    "Current password is incorrect");
            }

            user.PasswordHash = PasswordHelper.HashPassword(request.NewPassword);

            // Invalidate existing sessions so other devices must re-authenticate.
            user.RefreshToken = null;
            user.RefreshTokenExpiresAt = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepo.SaveChangesAsync();

            return Result.Success("Password changed successfully. Please log in again.");
        }

        public async Task<Result> ResendVerificationEmailAsync(ResendVerificationEmailRequest request)
        {
            var validationResult = await _resendVerificationValidator.ValidateAsync(request);
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
            var user = await _userRepo.GetByEmailAsync(normalizedEmail);

            // Only resend for accounts that exist and are not yet verified (inactive),
            // but never reveal whether the email exists or its status.
            if (user != null && user.Status == UserStatus.inactive.ToString())
            {
                var verifyToken = SecureTokenGenerator.Generate();

                user.EmailVerificationToken = verifyToken;
                user.UpdatedAt = DateTime.UtcNow;

                await _userRepo.UpdateAsync(user);

                var verifyLink = BuildVerifyLink(verifyToken);
                var body = _emailTemplateService.BuildVerifyEmailTemplate(user.FullName, verifyLink);

                await _emailService.SendEmailAsync(
                    user.Email,
                    "Verify your Sportico account",
                    body);
            }

            return Result.Success(ResendVerificationGenericMessage);
        }

        private string BuildVerifyLink(string token)
        {
            var apiBaseUrl = GetAppBaseUrl();
            return $"{apiBaseUrl}/api/auth/verify-email?token={Uri.EscapeDataString(token)}";
        }

        private string BuildResetLink(string token)
        {
            var apiBaseUrl = GetAppBaseUrl();
            return $"{apiBaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";
        }

        private string GetAppBaseUrl()
        {
            var apiBaseUrl = _configuration["AppSettings:ApiBaseUrl"];

            if (string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                throw new InvalidOperationException("AppSettings:ApiBaseUrl is missing.");
            }

            return apiBaseUrl.TrimEnd('/');
        }
    }
}
