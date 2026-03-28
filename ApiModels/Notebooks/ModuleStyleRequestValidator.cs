using FluentValidation;

namespace ApiModels.Notebooks;

public class ModuleStyleRequestValidator : AbstractValidator<ModuleStyleRequest>
{
    private const string HexPattern = @"^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$";

    private static readonly HashSet<string> ValidModuleTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Title", "Breadcrumb", "Subtitle", "Theory", "Practice", "Example",
        "Important", "Tip", "Homework", "Question", "ChordTablature", "FreeText"
    };

    private static readonly HashSet<string> ValidBorderStyles = new(StringComparer.OrdinalIgnoreCase)
    {
        "None", "Solid", "Dashed", "Dotted"
    };

    private static readonly HashSet<string> ValidFontFamilies = new(StringComparer.OrdinalIgnoreCase)
    {
        "Default", "Monospace", "Serif"
    };

    public ModuleStyleRequestValidator()
    {
        RuleFor(x => x.ModuleType)
            .NotEmpty()
            .Must(v => ValidModuleTypes.Contains(v))
            .WithMessage("ModuleType must be a valid ModuleType value.");

        RuleFor(x => x.BackgroundColor).NotEmpty().Matches(HexPattern)
            .WithMessage("BackgroundColor must be a valid hex colour (#RGB or #RRGGBB).");
        RuleFor(x => x.BorderColor).NotEmpty().Matches(HexPattern)
            .WithMessage("BorderColor must be a valid hex colour (#RGB or #RRGGBB).");
        RuleFor(x => x.HeaderBgColor).NotEmpty().Matches(HexPattern)
            .WithMessage("HeaderBgColor must be a valid hex colour (#RGB or #RRGGBB).");
        RuleFor(x => x.HeaderTextColor).NotEmpty().Matches(HexPattern)
            .WithMessage("HeaderTextColor must be a valid hex colour (#RGB or #RRGGBB).");
        RuleFor(x => x.BodyTextColor).NotEmpty().Matches(HexPattern)
            .WithMessage("BodyTextColor must be a valid hex colour (#RGB or #RRGGBB).");

        RuleFor(x => x.BorderStyle)
            .NotEmpty()
            .Must(v => ValidBorderStyles.Contains(v))
            .WithMessage("BorderStyle must be one of: None, Solid, Dashed, Dotted.");

        RuleFor(x => x.FontFamily)
            .NotEmpty()
            .Must(v => ValidFontFamilies.Contains(v))
            .WithMessage("FontFamily must be one of: Default, Monospace, Serif.");

        RuleFor(x => x.BorderWidth)
            .InclusiveBetween(0, 20)
            .WithMessage("BorderWidth must be between 0 and 20.");

        RuleFor(x => x.BorderRadius)
            .InclusiveBetween(0, 50)
            .WithMessage("BorderRadius must be between 0 and 50.");
    }
}