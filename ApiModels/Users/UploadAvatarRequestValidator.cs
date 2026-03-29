using FluentValidation;

namespace ApiModels.Users;

public class UploadAvatarRequestValidator : AbstractValidator<UploadAvatarRequest>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    public UploadAvatarRequestValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("A file is required.")
            .WithErrorCode("INVALID_FILE");

        When(x => x.File != null, () =>
        {
            RuleFor(x => x.File.Length)
                .GreaterThan(0).WithMessage("A file is required.")
                .WithErrorCode("INVALID_FILE")
                .LessThanOrEqualTo(2_097_152).WithMessage("File must not exceed 2 MB.")
                .WithErrorCode("FILE_TOO_LARGE");

            RuleFor(x => x.File.ContentType)
                .Must(ct => AllowedContentTypes.Contains(ct))
                .WithMessage("File must be a JPEG, PNG, or WebP image.")
                .WithErrorCode("INVALID_FILE_TYPE");
        });
    }
}
