using FluentValidation;
using SporticoApp.Application.DTOs.Community;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Application.Validators.Community
{
    public class CreateCommunityPostRequestValidator : AbstractValidator<CreateCommunityPostRequest>
    {
        public CreateCommunityPostRequestValidator()
        {
            RuleFor(x => x.PostType).NotEmpty().Must(t => CommunityPostTypes.All.Contains(t))
                .WithMessage($"postType must be one of: {string.Join(", ", CommunityPostTypes.All)}");
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Content).NotEmpty().MaximumLength(5000);
            RuleFor(x => x.LocationName).MaximumLength(200);
            RuleFor(x => x.Address).MaximumLength(300);
            RuleFor(x => x.Level).MaximumLength(30);
            RuleFor(x => x.FeePerPerson).GreaterThanOrEqualTo(0).When(x => x.FeePerPerson.HasValue);

            RuleFor(x => x)
                .Must(x => !x.StartAt.HasValue || !x.EndAt.HasValue || x.StartAt.Value < x.EndAt.Value)
                .WithMessage("startAt must be before endAt");

            // Recruitment post types require SportId + StartAt + MaxParticipants (includes the author).
            RuleFor(x => x.SportId).NotNull()
                .When(x => CommunityPostTypes.IsRecruitment(x.PostType))
                .WithMessage("sportId is required for this post type");
            RuleFor(x => x.StartAt).NotNull()
                .When(x => CommunityPostTypes.IsRecruitment(x.PostType))
                .WithMessage("startAt is required for this post type");
            RuleFor(x => x.MaxParticipants).NotNull().GreaterThanOrEqualTo(2)
                .When(x => CommunityPostTypes.IsRecruitment(x.PostType))
                .WithMessage("maxParticipants (>= 2, including the author) is required for this post type");

            RuleFor(x => x.Media).Must(m => m == null || m.Count <= 8)
                .WithMessage("A post may have at most 8 media items");
            RuleFor(x => x.Media)
                .Must(m => m == null || m.Count(x => x.MediaType == CommunityMediaTypes.Video) <= 1)
                .WithMessage("A post may have at most 1 video");
            RuleForEach(x => x.Media).SetValidator(new CommunityPostMediaRequestValidator());
        }
    }

    public class UpdateCommunityPostRequestValidator : AbstractValidator<UpdateCommunityPostRequest>
    {
        public UpdateCommunityPostRequestValidator()
        {
            RuleFor(x => x.Title).MaximumLength(200).When(x => x.Title != null);
            RuleFor(x => x.Content).MaximumLength(5000).When(x => x.Content != null);
            RuleFor(x => x.LocationName).MaximumLength(200);
            RuleFor(x => x.Address).MaximumLength(300);
            RuleFor(x => x.Level).MaximumLength(30);
            RuleFor(x => x.FeePerPerson).GreaterThanOrEqualTo(0).When(x => x.FeePerPerson.HasValue);
            RuleFor(x => x.MaxParticipants).GreaterThanOrEqualTo(1).When(x => x.MaxParticipants.HasValue);
            RuleFor(x => x)
                .Must(x => !x.StartAt.HasValue || !x.EndAt.HasValue || x.StartAt.Value < x.EndAt.Value)
                .WithMessage("startAt must be before endAt");
            RuleFor(x => x.Media).Must(m => m == null || m.Count <= 8)
                .WithMessage("A post may have at most 8 media items");
            RuleFor(x => x.Media)
                .Must(m => m == null || m.Count(x => x.MediaType == CommunityMediaTypes.Video) <= 1)
                .WithMessage("A post may have at most 1 video");
            RuleForEach(x => x.Media).SetValidator(new CommunityPostMediaRequestValidator());
        }
    }

    public class CommunityPostMediaRequestValidator : AbstractValidator<CommunityPostMediaRequest>
    {
        public CommunityPostMediaRequestValidator()
        {
            RuleFor(x => x.MediaType).NotEmpty().Must(t => t == CommunityMediaTypes.Image || t == CommunityMediaTypes.Video)
                .WithMessage("mediaType must be 'image' or 'video'");
            RuleFor(x => x.Url).NotEmpty()
                .Must(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
                .WithMessage("Media url must be an absolute https URL");
        }
    }

    public class CommunityPostFilterRequestValidator : AbstractValidator<CommunityPostFilterRequest>
    {
        private static readonly string[] SortValues = { "latest", "upcoming", "most_discussed" };

        public CommunityPostFilterRequestValidator()
        {
            RuleFor(x => x.PostType).Must(t => t == null || CommunityPostTypes.All.Contains(t));
            RuleFor(x => x.SortBy).Must(s => SortValues.Contains(s));
            RuleFor(x => x.PageNumber).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}
