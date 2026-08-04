using FluentValidation;
using SporticoApp.Application.DTOs.Chat;

namespace SporticoApp.Application.Validators.Chat
{
    public class CreateChatRoomRequestValidator
        : AbstractValidator<CreateChatRoomRequest>
    {
        public CreateChatRoomRequestValidator()
        {
            RuleFor(x => x)
                .Must(x => x.TargetUserId.HasValue || x.CoachId.HasValue)
                .WithMessage("targetUserId is required");

            RuleFor(x => x.SourceType)
                .MaximumLength(30)
                .When(x => x.SourceType != null);
        }
    }
}
