using ApiModels.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace ApiModels.Auth;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator(IStringLocalizer<ValidationMessages> localizer)
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(_ => localizer["EmailRequired"])
            .EmailAddress().WithMessage(_ => localizer["EmailInvalid"])
            .MaximumLength(256).WithMessage(_ => localizer["EmailTooLong"]);

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage(_ => localizer["DisplayNameRequired"])
            .MaximumLength(100).WithMessage(_ => localizer["DisplayNameTooLong"]);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(_ => localizer["PasswordRequired"])
            .MinimumLength(8).WithMessage(_ => localizer["PasswordTooShort"]);
    }
}
