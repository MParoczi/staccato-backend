namespace ApiModels.Users;

public record PresetResponse(Guid Id, string Name, List<StyleEntryDto> Styles);