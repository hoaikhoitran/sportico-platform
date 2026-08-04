using SporticoApp.Application.DTOs.Users;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Services
{
    public class UserBlockService : IUserBlockService
    {
        private readonly IUserBlockRepository _blockRepository;
        private readonly IUserRepository _userRepository;

        public UserBlockService(IUserBlockRepository blockRepository, IUserRepository userRepository)
        {
            _blockRepository = blockRepository;
            _userRepository = userRepository;
        }

        public async Task<Result<object>> BlockAsync(Guid blockerId, Guid targetUserId, BlockUserRequest request)
        {
            if (blockerId == targetUserId)
            {
                throw new ForbiddenException(ErrorCodes.UserBlockCannotBlockSelf, "You cannot block yourself");
            }

            var target = await _userRepository.GetByIdAsync(targetUserId);
            if (target == null)
            {
                throw new NotFoundException(ErrorCodes.UserNotFound, "User not found");
            }

            var existing = await _blockRepository.GetAsync(blockerId, targetUserId);
            if (existing != null)
            {
                // Idempotent: already blocked.
                return Result<object>.Success(new { blocked = true });
            }

            await _blockRepository.AddAsync(new UserBlock
            {
                BlockerId = blockerId,
                BlockedUserId = targetUserId,
                Reason = request.Reason,
                CreatedAt = DateTime.UtcNow
            });

            return Result<object>.Success(new { blocked = true });
        }

        public async Task<Result<object>> UnblockAsync(Guid blockerId, Guid targetUserId)
        {
            var existing = await _blockRepository.GetAsync(blockerId, targetUserId);
            if (existing == null)
            {
                // Idempotent: already not blocked.
                return Result<object>.Success(new { blocked = false });
            }

            await _blockRepository.RemoveAsync(existing);

            return Result<object>.Success(new { blocked = false });
        }

        public async Task<Result<List<BlockedUserResponse>>> GetBlockedAsync(Guid blockerId)
        {
            var blocks = await _blockRepository.GetBlockedByUserAsync(blockerId);

            var response = blocks.Select(b => new BlockedUserResponse
            {
                UserId = b.BlockedUserId,
                FullName = b.BlockedUser?.FullName ?? string.Empty,
                AvatarUrl = b.BlockedUser?.AvatarUrl,
                CreatedAt = b.CreatedAt,
                Reason = b.Reason
            }).ToList();

            return Result<List<BlockedUserResponse>>.Success(response);
        }
    }
}
