using FluentValidation;
using SporticoApp.Application.DTOs.Community;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Mappings;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Services
{
    using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

    /// <summary>
    /// Reports content into the SHARED Report table (same one reviews already use) — see
    /// SporticoApp.Shared.Constants.ReportTargetTypes. Deliberately not a second report system.
    /// </summary>
    public class CommunityReportService : ICommunityReportService
    {
        private readonly ICommunityReportRepository _reportRepository;
        private readonly ICommunityPostRepository _postRepository;
        private readonly ICommunityCommentRepository _commentRepository;
        private readonly IValidator<CreateReportRequest> _validator;

        public CommunityReportService(
            ICommunityReportRepository reportRepository,
            ICommunityPostRepository postRepository,
            ICommunityCommentRepository commentRepository,
            IValidator<CreateReportRequest> validator)
        {
            _reportRepository = reportRepository;
            _postRepository = postRepository;
            _commentRepository = commentRepository;
            _validator = validator;
        }

        public async Task<Result<ReportResponse>> CreateAsync(Guid reporterId, CreateReportRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    validationResult.Errors.Select(x => x.ErrorMessage).ToList());
            }

            if (request.TargetType == ReportTargetTypes.CommunityPost)
            {
                var post = await _postRepository.GetByIdAsync(request.TargetId);
                if (post == null)
                {
                    throw new NotFoundException(ErrorCodes.CommunityPostNotFound, "Post not found");
                }
            }
            else if (request.TargetType == ReportTargetTypes.CommunityComment)
            {
                var comment = await _commentRepository.GetByIdForUpdateAsync(request.TargetId);
                if (comment == null)
                {
                    throw new NotFoundException(ErrorCodes.CommunityCommentNotFound, "Comment not found");
                }
            }
            // chat_message: no dedicated lookup repository method exists yet — the targetId is
            // trusted as-is (validated as non-empty). See docs/community-api.md limitations.

            var existing = await _reportRepository.GetOpenReportAsync(request.TargetType, request.TargetId, reporterId);
            if (existing != null)
            {
                return Result<ReportResponse>.Success(existing.ToResponse()); // idempotent
            }

            var report = new Report
            {
                Id = Guid.NewGuid(),
                reporter_id = reporterId,
                TargetType = request.TargetType,
                TargetId = request.TargetId,
                Reason = request.Reason.Trim(),
                Description = request.Description,
                Status = ReportStatuses.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _reportRepository.AddWithoutSaveAsync(report);
            await _reportRepository.SaveChangesAsync();

            return Result<ReportResponse>.Success(report.ToResponse());
        }
    }
}
