using FluentValidation;
using SporticoApp.Application.DTOs.Users;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Mappings;
using SporticoApp.Core.Entities;
using SporticoApp.Core.Enums;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using SporticoApp.Shared.Helpers;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Services
{
    using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

    public class AdminUserService : IAdminUserService
    {
        private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            UserStatus.active.ToString(),
            UserStatus.inactive.ToString(),
            UserStatus.banned.ToString(),
            UserStatus.pending.ToString()
        };

        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IValidator<AdminUserFilterRequest> _filterValidator;
        private readonly IValidator<AdminCreateUserRequest> _createValidator;
        private readonly IValidator<AdminUpdateUserRequest> _updateValidator;

        public AdminUserService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IUserRoleRepository userRoleRepository,
            IValidator<AdminUserFilterRequest> filterValidator,
            IValidator<AdminCreateUserRequest> createValidator,
            IValidator<AdminUpdateUserRequest> updateValidator)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _userRoleRepository = userRoleRepository;
            _filterValidator = filterValidator;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        // ─────────────────────────────────────────────────────────────────────
        // List
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<PagedResult<AdminUserResponse>>> GetAllAsync(AdminUserFilterRequest filter)
        {
            await ValidateAsync(_filterValidator, filter);

            var (items, totalCount) = await _userRepository.GetPagedForAdminAsync(filter);

            var response = new PagedResult<AdminUserResponse>(
                items.Select(u => u.ToAdminUserResponse()).ToList(),
                totalCount,
                filter.PageNumber,
                filter.PageSize);

            return Result<PagedResult<AdminUserResponse>>.Success(response);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Detail
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<AdminUserResponse>> GetByIdAsync(Guid id)
        {
            var user = await _userRepository.GetByIdWithProfilesAndRolesAsync(id);
            if (user == null)
            {
                throw new NotFoundException(ErrorCodes.UserNotFound, "User not found");
            }

            return Result<AdminUserResponse>.Success(user.ToAdminUserResponse());
        }

        // ─────────────────────────────────────────────────────────────────────
        // Create
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<AdminUserResponse>> CreateAsync(AdminCreateUserRequest request)
        {
            await ValidateAsync(_createValidator, request);

            var email = request.Email.Trim().ToLowerInvariant();
            var status = NormalizeStatus(request.Status);

            // Validate roles exist BEFORE creating anything.
            var roleIds = await ResolveRoleIdsAsync(request.Roles);

            if (await _userRepository.ExistsByEmailAsync(email))
            {
                throw new ConflictException(
                    ErrorCodes.EmailAlreadyExists,
                    "A user with this email already exists");
            }

            var now = DateTime.UtcNow;
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = PasswordHelper.HashPassword(request.Password),
                FullName = request.FullName.Trim(),
                Phone = Normalize(request.Phone),
                AvatarUrl = Normalize(request.AvatarUrl),
                DateOfBirth = request.DateOfBirth,
                Status = status,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _userRepository.AddWithoutSaveAsync(user);
            foreach (var roleId in roleIds)
            {
                await _userRoleRepository.AddWithoutSaveAsync(new UserRole
                {
                    UserId = user.Id,
                    RoleId = roleId,
                    CreatedAt = now
                });
            }

            await _userRepository.SaveChangesAsync();

            var created = await _userRepository.GetByIdWithProfilesAndRolesAsync(user.Id);
            return Result<AdminUserResponse>.Success((created ?? user).ToAdminUserResponse());
        }

        // ─────────────────────────────────────────────────────────────────────
        // Update (basic info + optional role replacement). Email/password unchanged.
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<AdminUserResponse>> UpdateAsync(Guid id, AdminUpdateUserRequest request)
        {
            await ValidateAsync(_updateValidator, request);

            var status = NormalizeStatus(request.Status);

            // Resolve roles up-front so we never partially modify the user on a bad role.
            List<int>? desiredRoleIds = null;
            if (request.Roles != null)
            {
                desiredRoleIds = await ResolveRoleIdsAsync(request.Roles);
            }

            var user = await _userRepository.GetByIdForAdminUpdateAsync(id);
            if (user == null)
            {
                throw new NotFoundException(ErrorCodes.UserNotFound, "User not found");
            }

            user.FullName = request.FullName.Trim();
            user.Phone = Normalize(request.Phone);
            user.AvatarUrl = Normalize(request.AvatarUrl);
            user.DateOfBirth = request.DateOfBirth;
            user.Status = status;
            user.UpdatedAt = DateTime.UtcNow;

            if (desiredRoleIds != null)
            {
                ReplaceRoles(user, desiredRoleIds);
            }

            await _userRepository.SaveChangesAsync();

            var updated = await _userRepository.GetByIdWithProfilesAndRolesAsync(id);
            return Result<AdminUserResponse>.Success((updated ?? user).ToAdminUserResponse());
        }

        // ─────────────────────────────────────────────────────────────────────
        // Delete = status-based deactivation (preserves relational business data)
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<AdminUserResponse>> DeleteAsync(Guid id)
        {
            var user = await _userRepository.GetByIdForAdminUpdateAsync(id);
            if (user == null)
            {
                throw new NotFoundException(ErrorCodes.UserNotFound, "User not found");
            }

            // No physical delete: deactivate so bookings/payments/reviews/sessions/chat/wallet
            // foreign keys stay intact.
            user.Status = UserStatus.inactive.ToString();
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.SaveChangesAsync();

            var updated = await _userRepository.GetByIdWithProfilesAndRolesAsync(id);
            return Result<AdminUserResponse>.Success((updated ?? user).ToAdminUserResponse());
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Helpers
        // ═══════════════════════════════════════════════════════════════════════

        private static void ReplaceRoles(User user, List<int> desiredRoleIds)
        {
            var desired = desiredRoleIds.ToHashSet();
            var current = user.UserRoles.Select(ur => ur.RoleId).ToHashSet();

            // Remove roles no longer desired (EF deletes the user_roles rows).
            foreach (var toRemove in user.UserRoles.Where(ur => !desired.Contains(ur.RoleId)).ToList())
            {
                user.UserRoles.Remove(toRemove);
            }

            // Add newly desired roles.
            var now = DateTime.UtcNow;
            foreach (var roleId in desired.Where(rid => !current.Contains(rid)))
            {
                user.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = roleId,
                    CreatedAt = now
                });
            }
        }

        private async Task<List<int>> ResolveRoleIdsAsync(IEnumerable<string> roleNames)
        {
            var normalized = roleNames
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();

            var ids = new List<int>();
            foreach (var name in normalized)
            {
                var role = await _roleRepository.GetByNameAsync(name);
                if (role == null)
                {
                    throw new NotFoundException(
                        ErrorCodes.RoleNotFound,
                        $"Role '{name}' does not exist");
                }

                ids.Add(role.Id);
            }

            return ids;
        }

        private static string NormalizeStatus(string status)
        {
            var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();
            if (!AllowedStatuses.Contains(normalized))
            {
                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid user status",
                    new List<string> { $"Allowed statuses: {string.Join(", ", AllowedStatuses)}" });
            }

            return normalized;
        }

        private static string? Normalize(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static async Task ValidateAsync<T>(IValidator<T> validator, T request)
        {
            var result = await validator.ValidateAsync(request);
            if (!result.IsValid)
            {
                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    result.Errors.Select(e => e.ErrorMessage).ToList());
            }
        }
    }
}
