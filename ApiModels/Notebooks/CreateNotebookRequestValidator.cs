using FluentValidation;

namespace ApiModels.Notebooks;

public class CreateNotebookRequestValidator : AbstractValidator<CreateNotebookRequest>
{
    private const string HexPattern = @"^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$";

    private static readonly HashSet<string> ValidPageSizes = new(StringComparer.OrdinalIgnoreCase)
    {
        "A4", "A5", "A6", "B5", "B6"
    };

    private static readonly HashSet<string> ValidModuleTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Title", "Breadcrumb", "Subtitle", "Theory", "Practice", "Example",
        "Important", "Tip", "Homework", "Question", "ChordTablature", "FreeText"
    };

    public CreateNotebookRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.InstrumentId)
            .NotEmpty().WithMessage("InstrumentId is required.");

        RuleFor(x => x.PageSize)
            .NotEmpty().WithMessage("PageSize is required.")
            .Must(v => ValidPageSizes.Contains(v))
            .WithMessage("PageSize must be one of: A4, A5, A6, B5, B6.");

        RuleFor(x => x.CoverColor)
            .NotEmpty().WithMessage("CoverColor is required.")
            .Matches(HexPattern).WithMessage("CoverColor must be a valid hex colour (#RGB or #RRGGBB).");

        When(x => x.Styles != null, () =>
        {
            RuleFor(x => x.Styles!)
                .Must(s => s.Count == 12 &&
                           s.Select(e => e.ModuleType)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .Count() == 12 &&
                           s.All(e => ValidModuleTypes.Contains(e.ModuleType)))
                .WithMessage(
                    "Styles must contain exactly 12 items, one per ModuleType, with no duplicates.");

            RuleForEach(x => x.Styles!).SetValidator(new ModuleStyleRequestValidator());
        });
    }
}
