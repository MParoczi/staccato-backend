using FluentValidation;

namespace ApiModels.Users;

public class SavePresetRequestValidator : AbstractValidator<SavePresetRequest>
{
    private static readonly HashSet<string> ValidModuleTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Title", "Breadcrumb", "Subtitle", "Theory", "Practice",
        "Example", "Important", "Tip", "Homework", "Question",
        "ChordTablature", "FreeText"
    };

    public SavePresetRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Styles)
            .NotNull().WithMessage("Styles is required.")
            .Must(s => s != null && s.Count == 12).WithMessage("Exactly 12 style entries required.")
            .Must(entries => entries.All(e => ValidModuleTypes.Contains(e.ModuleType)))
            .WithMessage("Each moduleType must be a valid ModuleType value.")
            .Must(entries => entries.Select(e => e.ModuleType.ToLowerInvariant()).Distinct().Count() == entries.Count)
            .WithMessage("Duplicate moduleType values are not allowed.")
            .Must(entries => entries.All(e => !string.IsNullOrEmpty(e.StylesJson)))
            .WithMessage("Each stylesJson must be a non-empty string.");
    }
}