using ApiModels.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace ApiModels.Auth;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator(IStringLocalizer<ValidationMessages> localizer)
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(_ => localizer["EmailRequired"])
            .EmailAddress().WithMessage(_ => localizer["EmailInvalid"]);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(_ => localizer["PasswordRequired"]);

        // RememberMe: no validation rule — absent/false are both valid
    }
}