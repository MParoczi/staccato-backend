using FluentValidation;

namespace ApiModels.Exports;

public class CreatePdfExportRequestValidator : AbstractValidator<CreatePdfExportRequest>
{
    public CreatePdfExportRequestValidator()
    {
        RuleFor(x => x.NotebookId)
            .NotEmpty().WithMessage("NotebookId is required.");

        When(x => x.LessonIds != null, () =>
        {
            RuleFor(x => x.LessonIds!)
                .Must(ids => ids.Count > 0)
                .WithMessage("LessonIds must not be an empty list.");

            RuleForEach(x => x.LessonIds!)
                .NotEmpty().WithMessage("Each lesson ID must not be empty.");
        });
    }
}
