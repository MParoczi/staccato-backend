using System.Text.Json;
using ApiModels.Users;
using AutoMapper;
using DomainModels.Enums;
using DomainModels.Models;

namespace Api.Mapping;

public class DomainToResponseProfile : Profile
{
    public DomainToResponseProfile()
    {
        CreateMap<User, UserResponse>()
            .ConstructUsing((s, _) => new UserResponse(
                s.Id,
                s.Email,
                s.FirstName,
                s.LastName,
                s.Language == Language.English ? "en" : "hu",
                s.DefaultPageSize != null ? s.DefaultPageSize.ToString() : null,
                s.DefaultInstrumentId,
                s.AvatarUrl,
                s.ScheduledDeletionAt))
            .ForMember(dest => dest.Language, opt => opt.Ignore())
            .ForMember(dest => dest.DefaultPageSize, opt => opt.Ignore());

        CreateMap<UserSavedPreset, PresetResponse>()
            .ConstructUsing((s, _) => new PresetResponse(
                s.Id,
                s.Name,
                JsonSerializer.Deserialize<List<StyleEntryDto>>(s.StylesJson, (JsonSerializerOptions?)null)
                    ?? new List<StyleEntryDto>()));
    }
}
