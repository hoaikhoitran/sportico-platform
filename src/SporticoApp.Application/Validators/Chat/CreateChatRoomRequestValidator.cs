using FluentValidation;
using SporticoApp.Application.DTOs.Chat;

namespace SporticoApp.Application.Validators.Chat
{
    public class CreateChatRoomRequestValidator
        : AbstractValidator<CreateChatRoomRequest>
    {
        public CreateChatRoomRequestValidator()
        {
            RuleFor(x => x.CoachId)
                .NotEmpty()
                .WithMessage("CoachId is required");
        }
    }
}
