using FluentValidation;
using SporticoApp.Application.DTOs.Chat;

namespace SporticoApp.Application.Validators.Chat
{
    public class SendMessageRequestValidator
        : AbstractValidator<SendMessageRequest>
    {
        public SendMessageRequestValidator()
        {
            RuleFor(x => x.Content)
                .MaximumLength(2000)
                .WithMessage("Content is too long");

            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.Content) || (x.Attachments != null && x.Attachments.Count > 0))
                .WithMessage("Either content or at least one attachment is required");

            RuleFor(x => x.Attachments)
                .Must(a => a == null || a.Count <= 5)
                .WithMessage("A message may have at most 5 attachments");

            RuleForEach(x => x.Attachments).ChildRules(a =>
            {
                a.RuleFor(x => x.FileUrl)
                    .NotEmpty()
                    .Must(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) &&
                               (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
                    .WithMessage("Attachment fileUrl must be a valid http(s) URL");
            });
        }
    }
}
