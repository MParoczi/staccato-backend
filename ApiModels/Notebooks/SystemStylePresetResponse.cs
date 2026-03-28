namespace ApiModels.Notebooks;

public record SystemStylePresetResponse(
    Guid Id,
    string Name,
    int DisplayOrder,
    bool IsDefault,
    List<ModuleStyleResponse> Styles
);
