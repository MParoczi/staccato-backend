using System.Text.Json;
using FluentValidation;

namespace ApiModels.Modules;

public class CreateModuleRequestValidator : AbstractValidator<CreateModuleRequest>
{
    private static readonly HashSet<string> ValidModuleTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Title", "Breadcrumb", "Subtitle", "Theory", "Practice", "Example",
        "Important", "Tip", "Homework", "Question", "ChordTablature", "FreeText"
    };

    public CreateModuleRequestValidator()
    {
        RuleFor(x => x.ModuleType)
            .NotEmpty().WithMessage("ModuleType is required.")
            .Must(v => ValidModuleTypes.Contains(v))
            .WithMessage("ModuleType must be a valid module type.");

        RuleFor(x => x.GridX)
            .GreaterThanOrEqualTo(0).WithMessage("GridX must be >= 0.");

        RuleFor(x => x.GridY)
            .GreaterThanOrEqualTo(0).WithMessage("GridY must be >= 0.");

        RuleFor(x => x.GridWidth)
            .GreaterThanOrEqualTo(1).WithMessage("GridWidth must be >= 1.");

        RuleFor(x => x.GridHeight)
            .GreaterThanOrEqualTo(1).WithMessage("GridHeight must be >= 1.");

        RuleFor(x => x.ZIndex)
            .GreaterThanOrEqualTo(0).WithMessage("ZIndex must be >= 0.");

        RuleFor(x => x.Content)
            .Must(c => c.ValueKind == JsonValueKind.Array && c.GetArrayLength() == 0)
            .WithMessage("Content must be an empty array for new modules.");
    }
}
