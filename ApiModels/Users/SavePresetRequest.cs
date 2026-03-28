namespace ApiModels.Users;

public record SavePresetRequest(string Name, IList<StyleEntryDto> Styles);