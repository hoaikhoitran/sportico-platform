using FluentValidation;
using SporticoApp.Application.DTOs.Auth;

namespace SporticoApp.Application.Validators.Auth
{
    public class GoogleIdTokenLoginRequestValidator : AbstractValidator<GoogleIdTokenLoginRequest>
    {
        /// <summary>
        /// Google ID tokens are well under 4 KB. The cap exists so an oversized body is rejected by
        /// validation before it ever reaches signature verification.
        /// </summary>
        private const int MaxIdTokenLength = 8192;

        public GoogleIdTokenLoginRequestValidator()
        {
            RuleFor(x => x.IdToken)
                .NotEmpty().WithMessage("idToken is required")
                .MaximumLength(MaxIdTokenLength).WithMessage("idToken is too long");
        }
    }

    public class GoogleExchangeCodeRequestValidator : AbstractValidator<GoogleExchangeCodeRequest>
    {
        /// <summary>32 random bytes base64url = 43 chars; allow headroom without accepting junk.</summary>
        private const int MaxCodeLength = 256;

        public GoogleExchangeCodeRequestValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("code is required")
                .MaximumLength(MaxCodeLength).WithMessage("code is too long");
        }
    }
}
