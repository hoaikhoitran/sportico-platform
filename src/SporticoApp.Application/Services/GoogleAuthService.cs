using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Microsoft.Extensions.Options;
using SporticoApp.Application.DTOs.Auth;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Options;
using SporticoApp.Core.Entities;
using SporticoApp.Core.Enums;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Services
{
    using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

    /// <summary>
    /// Account resolution + linking for Google sign-in, shared by both flows:
    /// the ID-token flow (POST /api/auth/google) and the browser-redirect flow
    /// (GET /api/auth/google → callback → exchange code).
    /// <para>
    /// Identity always arrives already verified — either from <see cref="IGoogleIdentityProvider"/>
    /// or from the Google OAuth handler. This service never parses a token itself.
    /// </para>
    /// </summary>
    public class GoogleAuthService : IGoogleAuthService
    {
        /// <summary>32 random bytes, base64url — ~256 bits of entropy, URL-safe without escaping.</summary>
        private const int ExchangeCodeByteLength = 32;

        private readonly IGoogleIdentityProvider _googleIdentityProvider;
        private readonly IUserRepository _userRepo;
        private readonly IRoleRepository _roleRepo;
        private readonly IUserRoleRepository _userRoleRepo;
        private readonly IUserExternalLoginRepository _externalLoginRepo;
        private readonly IAuthExchangeCodeRepository _exchangeCodeRepo;
        private readonly ITokenIssuer _tokenIssuer;
        private readonly GoogleAuthOptions _options;
        private readonly IValidator<GoogleIdTokenLoginRequest> _idTokenValidator;
        private readonly IValidator<GoogleExchangeCodeRequest> _exchangeValidator;

        public GoogleAuthService(
            IGoogleIdentityProvider googleIdentityProvider,
            IUserRepository userRepo,
            IRoleRepository roleRepo,
            IUserRoleRepository userRoleRepo,
            IUserExternalLoginRepository externalLoginRepo,
            IAuthExchangeCodeRepository exchangeCodeRepo,
            ITokenIssuer tokenIssuer,
            IOptions<GoogleAuthOptions> options,
            IValidator<GoogleIdTokenLoginRequest> idTokenValidator,
            IValidator<GoogleExchangeCodeRequest> exchangeValidator)
        {
            _googleIdentityProvider = googleIdentityProvider;
            _userRepo = userRepo;
            _roleRepo = roleRepo;
            _userRoleRepo = userRoleRepo;
            _externalLoginRepo = externalLoginRepo;
            _exchangeCodeRepo = exchangeCodeRepo;
            _tokenIssuer = tokenIssuer;
            _options = options.Value;
            _idTokenValidator = idTokenValidator;
            _exchangeValidator = exchangeValidator;
        }

        // ── Flow A: Google Identity Services ID token ───────────────────────────

        public async Task<Result<LoginResponse>> LoginWithIdTokenAsync(GoogleIdTokenLoginRequest request)
        {
            await ValidateOrThrowAsync(_idTokenValidator, request);

            if (!_options.IsIdTokenFlowConfigured)
            {
                // Name the missing KEY only — never its value.
                throw new ServiceUnavailableException(
                    ErrorCodes.GoogleConfigurationMissing,
                    "Google sign-in is not configured on this environment.",
                    new List<string> { "GOOGLE_CLIENT_ID" });
            }

            var identity = await _googleIdentityProvider.VerifyIdTokenAsync(request.IdToken.Trim());

            var user = await ResolveUserAsync(identity);
            var response = await _tokenIssuer.IssueAsync(user);

            return Result<LoginResponse>.Success(response);
        }

        // ── Flow B: browser redirect ────────────────────────────────────────────

        public async Task<string> CreateExchangeCodeForIdentityAsync(GoogleIdentity identity)
        {
            var user = await ResolveUserAsync(identity);

            // CSPRNG. base64url so the value survives a query string untouched.
            var bytes = RandomNumberGenerator.GetBytes(ExchangeCodeByteLength);
            var plaintext = Base64UrlEncode(bytes);

            var now = DateTime.UtcNow;
            await _exchangeCodeRepo.AddAsync(new AuthExchangeCode
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CodeHash = Sha256Hex(plaintext),
                ExpiresAt = now.AddSeconds(_options.EffectiveExchangeCodeLifetimeSeconds),
                CreatedAt = now
            });

            // Opportunistic cleanup — cheap, indexed, and removes the need for a dedicated worker.
            await _exchangeCodeRepo.DeleteExpiredAsync(now);

            // The ONLY time the plaintext exists outside the caller's request.
            return plaintext;
        }

        public async Task<Result<LoginResponse>> ExchangeCodeAsync(GoogleExchangeCodeRequest request)
        {
            await ValidateOrThrowAsync(_exchangeValidator, request);

            var codeHash = Sha256Hex(request.Code.Trim());
            var now = DateTime.UtcNow;

            var consumed = await _exchangeCodeRepo.ConsumeAsync(codeHash, now);
            if (consumed == null)
            {
                // The atomic consume matched nothing. Look the row up to return a precise reason;
                // a losing concurrent request lands here and gets ALREADY_USED, never a token.
                var existing = await _exchangeCodeRepo.FindAnyAsync(codeHash);

                if (existing == null)
                {
                    throw new UnauthorizedException(
                        ErrorCodes.GoogleExchangeCodeInvalid,
                        "Invalid Google authentication exchange code");
                }

                if (existing.UsedAt != null)
                {
                    throw new ConflictException(
                        ErrorCodes.GoogleExchangeCodeAlreadyUsed,
                        "Google authentication exchange code was already used");
                }

                throw new UnauthorizedException(
                    ErrorCodes.GoogleExchangeCodeExpired,
                    "Google authentication exchange code has expired");
            }

            var user = await _userRepo.GetByIdWithRolesAsync(consumed.UserId);
            if (user == null)
            {
                throw new UnauthorizedException(
                    ErrorCodes.GoogleExchangeCodeInvalid,
                    "Invalid Google authentication exchange code");
            }

            EnsureUserCanSignIn(user);

            var response = await _tokenIssuer.IssueAsync(user);
            return Result<LoginResponse>.Success(response);
        }

        // ── Shared account resolution ───────────────────────────────────────────

        /// <summary>
        /// Resolves the Sportico user for a verified Google identity, creating or linking as needed.
        /// Returns a user loaded WITH roles, ready for token issuance.
        /// </summary>
        private async Task<User> ResolveUserAsync(GoogleIdentity identity)
        {
            if (string.IsNullOrWhiteSpace(identity.Subject))
            {
                throw new UnauthorizedException(
                    ErrorCodes.GoogleInvalidToken,
                    "Google authentication failed");
            }

            if (string.IsNullOrWhiteSpace(identity.Email))
            {
                throw new UnauthorizedException(
                    ErrorCodes.GoogleInvalidToken,
                    "Google authentication failed");
            }

            // A Google account whose email Google itself has not verified must never be allowed to
            // take over a Sportico account that uses the same address.
            if (!identity.EmailVerified)
            {
                throw new UnauthorizedException(
                    ErrorCodes.GoogleEmailNotVerified,
                    "Your Google email address is not verified");
            }

            var normalizedEmail = identity.Email.Trim().ToLowerInvariant();

            // 1. Known Google subject → straight login.
            var existingLink = await _externalLoginRepo.GetByProviderSubjectAsync(
                ExternalAuthProviders.Google, identity.Subject);

            if (existingLink != null)
            {
                var linkedUser = await _userRepo.GetByIdWithRolesAsync(existingLink.UserId)
                    ?? throw new UnauthorizedException(
                        ErrorCodes.GoogleLoginFailed,
                        "Google authentication failed");

                EnsureUserCanSignIn(linkedUser);

                existingLink.LastLoginAt = DateTime.UtcNow;
                BackfillMissingProfileFields(linkedUser, identity);
                linkedUser.UpdatedAt = DateTime.UtcNow;

                await _externalLoginRepo.SaveChangesAsync();
                return linkedUser;
            }

            // 2. Unknown subject, but the (Google-verified) email already exists → link.
            var userByEmail = await _userRepo.GetByEmailWithRolesAsync(normalizedEmail);
            if (userByEmail != null)
            {
                return await LinkGoogleToExistingUserAsync(userByEmail, identity, normalizedEmail);
            }

            // 3. Brand new account.
            return await CreateGoogleUserAsync(identity, normalizedEmail);
        }

        private async Task<User> LinkGoogleToExistingUserAsync(
            User user, GoogleIdentity identity, string normalizedEmail)
        {
            // A banned account stays banned: never link, never activate, never issue a token.
            if (user.Status == UserStatus.banned.ToString())
            {
                throw new ForbiddenException(
                    ErrorCodes.AccountNotActive,
                    "This account has been suspended");
            }

            // One Sportico account may hold at most one Google link. A different subject on the
            // same email means two distinct Google accounts are fighting over one Sportico user.
            var linkForUser = await _externalLoginRepo.GetByUserAndProviderAsync(
                user.Id, ExternalAuthProviders.Google);

            if (linkForUser != null && linkForUser.ProviderSubject != identity.Subject)
            {
                throw new ConflictException(
                    ErrorCodes.GoogleAccountConflict,
                    "This Sportico account is already linked to a different Google account");
            }

            var now = DateTime.UtcNow;

            if (linkForUser == null)
            {
                await _externalLoginRepo.AddWithoutSaveAsync(new UserExternalLogin
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Provider = ExternalAuthProviders.Google,
                    ProviderSubject = identity.Subject,
                    ProviderEmail = normalizedEmail,
                    CreatedAt = now,
                    LastLoginAt = now
                });
            }
            else
            {
                linkForUser.LastLoginAt = now;
            }

            // Google has verified this address, so an account still waiting on email confirmation
            // is now proven. "pending" is a moderation state, not an email state — leave it alone.
            if (user.Status == UserStatus.inactive.ToString())
            {
                user.Status = UserStatus.active.ToString();
                user.EmailVerificationToken = null;
            }

            EnsureUserCanSignIn(user);

            BackfillMissingProfileFields(user, identity);
            user.UpdatedAt = now;

            await _externalLoginRepo.SaveChangesAsync();
            return user;
        }

        private async Task<User> CreateGoogleUserAsync(GoogleIdentity identity, string normalizedEmail)
        {
            var learnerRole = await _roleRepo.GetByNameAsync(RoleConstants.Learner)
                ?? throw new NotFoundException(ErrorCodes.RoleNotFound, "Learner role not found");

            var now = DateTime.UtcNow;
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                // No local password: this account can only sign in with Google until the user
                // sets one through the forgot/reset-password flow.
                PasswordHash = null,
                FullName = ResolveFullName(identity, normalizedEmail),
                AvatarUrl = SanitizeAvatarUrl(identity.AvatarUrl),
                // Google already verified the address, so skip the e-mail verification round trip.
                Status = UserStatus.active.ToString(),
                EmailVerificationToken = null,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _userRepo.AddWithoutSaveAsync(user);
            await _userRoleRepo.AddWithoutSaveAsync(new UserRole
            {
                UserId = user.Id,
                RoleId = learnerRole.Id,
                CreatedAt = now
            });
            await _externalLoginRepo.AddWithoutSaveAsync(new UserExternalLogin
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = ExternalAuthProviders.Google,
                ProviderSubject = identity.Subject,
                ProviderEmail = normalizedEmail,
                CreatedAt = now,
                LastLoginAt = now
            });

            try
            {
                // One save unit: user + role + external login commit together or not at all, so a
                // half-created account (user without learner role) is impossible.
                await _userRepo.SaveChangesAsync();
            }
            catch (Exception ex) when (IsUniqueViolation(ex))
            {
                // A concurrent first login for the same Google account won the race. Reload what it
                // created rather than surfacing a 500 or creating a duplicate.
                var winner = await ReloadAfterRaceAsync(identity, normalizedEmail);
                if (winner != null)
                {
                    return winner;
                }

                throw new ConflictException(
                    ErrorCodes.GoogleAccountConflict,
                    "This Google account could not be linked. Please try again.");
            }

            // Reload with roles so the JWT carries the learner claim.
            return await _userRepo.GetByIdWithRolesAsync(user.Id) ?? user;
        }

        /// <summary>
        /// After a unique-constraint race, fetch whatever the winning request persisted — by Google
        /// subject first, then by email.
        /// </summary>
        private async Task<User?> ReloadAfterRaceAsync(GoogleIdentity identity, string normalizedEmail)
        {
            var link = await _externalLoginRepo.GetByProviderSubjectAsync(
                ExternalAuthProviders.Google, identity.Subject);

            if (link != null)
            {
                var user = await _userRepo.GetByIdWithRolesAsync(link.UserId);
                if (user != null)
                {
                    EnsureUserCanSignIn(user);
                    return user;
                }
            }

            var byEmail = await _userRepo.GetByEmailWithRolesAsync(normalizedEmail);
            if (byEmail != null)
            {
                EnsureUserCanSignIn(byEmail);
                return byEmail;
            }

            return null;
        }

        // ── Guards and helpers ──────────────────────────────────────────────────

        private static void EnsureUserCanSignIn(User user)
        {
            if (user.Status == UserStatus.banned.ToString())
            {
                throw new ForbiddenException(
                    ErrorCodes.AccountNotActive,
                    "This account has been suspended");
            }

            if (user.Status != UserStatus.active.ToString())
            {
                throw new UnauthorizedException(
                    ErrorCodes.AccountNotActive,
                    "Account is not active, check your email to active your account.");
            }
        }

        /// <summary>
        /// Fills gaps only. A name or avatar the user has already set is theirs — Google must never
        /// overwrite it on a later sign-in.
        /// </summary>
        private static void BackfillMissingProfileFields(User user, GoogleIdentity identity)
        {
            if (string.IsNullOrWhiteSpace(user.FullName) && !string.IsNullOrWhiteSpace(identity.FullName))
            {
                user.FullName = identity.FullName.Trim();
            }

            if (string.IsNullOrWhiteSpace(user.AvatarUrl))
            {
                var avatar = SanitizeAvatarUrl(identity.AvatarUrl);
                if (avatar != null)
                {
                    user.AvatarUrl = avatar;
                }
            }
        }

        private static string ResolveFullName(GoogleIdentity identity, string normalizedEmail)
        {
            if (!string.IsNullOrWhiteSpace(identity.FullName))
            {
                var trimmed = identity.FullName.Trim();
                return trimmed.Length > 150 ? trimmed[..150] : trimmed;
            }

            // FullName is NOT NULL in the database; fall back to the local part of the email.
            var localPart = normalizedEmail.Split('@')[0];
            return string.IsNullOrWhiteSpace(localPart) ? "Sportico user" : localPart;
        }

        /// <summary>
        /// A malformed picture URL must never fail a login — it is cosmetic. Only absolute HTTPS
        /// URLs are accepted.
        /// </summary>
        private static string? SanitizeAvatarUrl(string? avatarUrl)
        {
            if (string.IsNullOrWhiteSpace(avatarUrl))
            {
                return null;
            }

            if (!Uri.TryCreate(avatarUrl.Trim(), UriKind.Absolute, out var uri))
            {
                return null;
            }

            return uri.Scheme == Uri.UriSchemeHttps ? uri.ToString() : null;
        }

        /// <summary>
        /// Detects a database unique-constraint violation without making the Application layer
        /// depend on Npgsql: EF wraps the provider error, whose SQLSTATE for unique_violation is
        /// 23505. Matching on the message keeps this provider-agnostic and testable.
        /// </summary>
        private static bool IsUniqueViolation(Exception ex)
        {
            for (var current = ex; current != null; current = current.InnerException)
            {
                var message = current.Message;
                if (message.Contains("23505", StringComparison.Ordinal) ||
                    message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
                    message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) ||
                    message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string Sha256Hex(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string Base64UrlEncode(byte[] bytes) =>
            Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

        private static async Task ValidateOrThrowAsync<T>(IValidator<T> validator, T instance)
        {
            var result = await validator.ValidateAsync(instance);
            if (!result.IsValid)
            {
                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    result.Errors.Select(x => x.ErrorMessage).ToList());
            }
        }
    }
}
