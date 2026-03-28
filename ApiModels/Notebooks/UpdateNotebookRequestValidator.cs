using FluentValidation;

namespace ApiModels.Notebooks;

public class UpdateNotebookRequestValidator : AbstractValidator<UpdateNotebookRequest>
{
    private const string HexPattern = @"^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$";

    public UpdateNotebookRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.CoverColor)
            .NotEmpty().WithMessage("CoverColor is required.")
            .Matches(HexPattern).WithMessage("CoverColor must be a valid hex colour (#RGB or #RRGGBB).");

        RuleFor(x => x.InstrumentId)
            .Must(v => v == null)
            .WithMessage("Instrument cannot be changed after creation.")
            .WithErrorCode("NOTEBOOK_INSTRUMENT_IMMUTABLE");

        RuleFor(x => x.PageSize)
            .Must(v => v == null)
            .WithMessage("Page size cannot be changed after creation.")
            .WithErrorCode("NOTEBOOK_PAGE_SIZE_IMMUTABLE");
    }
}