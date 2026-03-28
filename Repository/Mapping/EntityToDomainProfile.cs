using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using DomainModels.Models;
using EntityModels.Entities;

namespace Repository.Mapping;

public class EntityToDomainProfile : Profile
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public EntityToDomainProfile()
    {
        CreateMap<UserEntity, User>().ReverseMap();
        CreateMap<RefreshTokenEntity, RefreshToken>().ReverseMap();
        CreateMap<UserSavedPresetEntity, UserSavedPreset>().ReverseMap();
        CreateMap<InstrumentEntity, Instrument>().ReverseMap();
        CreateMap<ChordEntity, Chord>()
            .ForMember(d => d.InstrumentKey, o => o.MapFrom(s => s.Instrument.Key))
            .ForMember(d => d.Positions, o => o.MapFrom(s =>
                JsonSerializer.Deserialize<List<ChordPosition>>(s.PositionsJson, JsonOptions)
                ?? new List<ChordPosition>()));
        CreateMap<NotebookEntity, Notebook>()
            .ForMember(d => d.InstrumentName,
                o => o.MapFrom(s => s.Instrument != null ? s.Instrument.DisplayName : string.Empty))
            .ForMember(d => d.LessonCount,
                o => o.MapFrom(s => s.Lessons != null ? s.Lessons.Count : 0));
        CreateMap<Notebook, NotebookEntity>()
            .ForMember(d => d.User, o => o.Ignore())
            .ForMember(d => d.Instrument, o => o.Ignore())
            .ForMember(d => d.Lessons, o => o.Ignore())
            .ForMember(d => d.ModuleStyles, o => o.Ignore())
            .ForMember(d => d.PdfExports, o => o.Ignore());
        CreateMap<NotebookModuleStyleEntity, NotebookModuleStyle>().ReverseMap();
        CreateMap<SystemStylePresetEntity, SystemStylePreset>().ReverseMap();
        CreateMap<LessonEntity, Lesson>().ReverseMap();
        CreateMap<LessonPageEntity, LessonPage>().ReverseMap();
        CreateMap<ModuleEntity, Module>().ReverseMap();

        CreateMap<PdfExportEntity, PdfExport>()
            .ForMember(d => d.LessonIds, o => o.MapFrom(s =>
                s.LessonIdsJson == null
                    ? null
                    : JsonSerializer.Deserialize<List<Guid>>(s.LessonIdsJson)))
            .ReverseMap()
            .ForPath(s => s.LessonIdsJson, o => o.MapFrom(d =>
                d.LessonIds == null
                    ? null
                    : JsonSerializer.Serialize(d.LessonIds)));
    }
}