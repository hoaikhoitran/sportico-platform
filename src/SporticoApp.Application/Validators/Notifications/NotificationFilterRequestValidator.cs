using FluentValidation;
using SporticoApp.Application.DTOs.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Application.Validators.Notifications
{
    public class NotificationFilterRequestValidator
        : AbstractValidator<NotificationFilterRequest>
    {
        public NotificationFilterRequestValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Page number must be greater than or equal to 1");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Page size must be between 1 and 100");

            RuleFor(x => x.Type)
                .MaximumLength(50)
                .WithMessage("Notification type is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Type));
        }
    }
}
