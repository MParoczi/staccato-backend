using FluentValidation;

namespace ApiModels.Users;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    private static readonly string[] ValidLanguages = ["en", "hu"];
    private static readonly string[] ValidPageSizes = ["A4", "A5", "A6", "B5", "B6"];

    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("FirstName is required.")
            .MaximumLength(100).WithMessage("FirstName must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotNull().WithMessage("LastName is required.")
            .MaximumLength(100).WithMessage("LastName must not exceed 100 characters.");

        RuleFor(x => x.Language)
            .NotEmpty().WithMessage("Language is required.")
            .Must(v => ValidLanguages.Contains(v)).WithMessage("Language must be 'en' or 'hu'.");

        RuleFor(x => x.DefaultPageSize)
            .Must(v => v == null || ValidPageSizes.Contains(v))
            .WithMessage("DefaultPageSize must be one of: A4, A5, A6, B5, B6.");
    }
}