using FluentValidation;

namespace ApiModels.Modules;

public class PatchModuleLayoutRequestValidator : AbstractValidator<PatchModuleLayoutRequest>
{
    public PatchModuleLayoutRequestValidator()
    {
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
    }
}
