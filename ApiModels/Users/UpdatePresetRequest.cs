namespace ApiModels.Users;

public record UpdatePresetRequest(string? Name, IList<StyleEntryDto>? Styles);