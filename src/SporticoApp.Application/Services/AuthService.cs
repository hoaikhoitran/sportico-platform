using SporticoApp.Application.DTOs.Auth;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Core.Entities;
using SporticoApp.Core.Enums;
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
        public AuthService(
            IUserRepository userRepo, 
            IRoleRepository roleRepo, 
            IUserRoleRepository userRoleRepo, 
            IJwtService jwtService)
        {
            _userRepo = userRepo;
            _roleRepo = roleRepo;
            _userRoleRepo = userRoleRepo;
            _jwtService = jwtService;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {

        }

        public async Task<Result> RegisterAsync(RegisterRequest request)
        {
            var existingUser = await _userRepo.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return Result.Fail("Email is already registered");
            }

            var user = new User()
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = PasswordHelper.HashPassword(request.Password),
                Status = UserStatus.inactive,
                FullName = request.FullName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _userRepo.AddAsync(user);
            var leanerRole = await _roleRepo.GetByNameAsync("learner");

            var userRole = new UserRole()
            {
                UserId = user.Id,
                RoleId = leanerRole.Id
            };

            await _userRoleRepo.AddAsync(userRole);
            return Result.Success("Registration successful");
        }

    }
}
