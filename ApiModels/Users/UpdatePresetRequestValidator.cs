using FluentValidation;

namespace ApiModels.Users;

public class UpdatePresetRequestValidator : AbstractValidator<UpdatePresetRequest>
{
    private static readonly HashSet<string> ValidModuleTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Title", "Breadcrumb", "Subtitle", "Theory", "Practice",
        "Example", "Important", "Tip", "Homework", "Question",
        "ChordTablature", "FreeText"
    };

    public UpdatePresetRequestValidator()
    {
        RuleFor(x => x)
            .Must(r => r.Name != null || r.Styles != null)
            .WithMessage("At least one of name or styles must be provided.");

        When(x => x.Name != null, () =>
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name must not be empty.")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
        });

        When(x => x.Styles != null, () =>
        {
            RuleFor(x => x.Styles)
                .Must(s => s != null && s.Count == 12).WithMessage("Exactly 12 style entries required.")
                .Must(entries => entries!.All(e => ValidModuleTypes.Contains(e.ModuleType)))
                .WithMessage("Each moduleType must be a valid ModuleType value.")
                .Must(entries => entries!.Select(e => e.ModuleType.ToLowerInvariant()).Distinct().Count() == entries!.Count)
                .WithMessage("Duplicate moduleType values are not allowed.")
                .Must(entries => entries!.All(e => !string.IsNullOrEmpty(e.StylesJson)))
                .WithMessage("Each stylesJson must be a non-empty string.");
        });
    }
}