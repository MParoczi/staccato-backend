using ApiModels.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace ApiModels.Auth;

public class GoogleAuthRequestValidator : AbstractValidator<GoogleAuthRequest>
{
    public GoogleAuthRequestValidator(IStringLocalizer<ValidationMessages> localizer)
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage(_ => localizer["IdTokenRequired"]);
    }
}