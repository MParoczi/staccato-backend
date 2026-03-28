using System.Text.Json;
using ApiModels.Users;
using AutoMapper;
using DomainModels.Models;

namespace Api.Mapping;

public class DomainToResponseProfile : Profile
{
    public DomainToResponseProfile()
    {
        CreateMap<User, UserResponse>()
            .ForMember(d => d.Language, o => o.MapFrom(s => s.Language.ToString()))
            .ForMember(d => d.DefaultPageSize, o => o.MapFrom(s => s.DefaultPageSize != null ? s.DefaultPageSize.ToString() : null));

        CreateMap<UserSavedPreset, PresetResponse>()
            .ForMember(d => d.Styles, o => o.MapFrom(s =>
                JsonSerializer.Deserialize<List<StyleEntryDto>>(s.StylesJson, (JsonSerializerOptions?)null)));
    }
}
