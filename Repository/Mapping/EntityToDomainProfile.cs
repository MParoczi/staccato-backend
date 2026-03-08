using AutoMapper;
using DomainModels.Models;
using EntityModels.Entities;
using System.Text.Json;

namespace Repository.Mapping;

public class EntityToDomainProfile : Profile
{
    public EntityToDomainProfile()
    {
        CreateMap<UserEntity, User>().ReverseMap();
        CreateMap<RefreshTokenEntity, RefreshToken>().ReverseMap();
        CreateMap<UserSavedPresetEntity, UserSavedPreset>().ReverseMap();
        CreateMap<InstrumentEntity, Instrument>().ReverseMap();
        CreateMap<ChordEntity, Chord>().ReverseMap();
        CreateMap<NotebookEntity, Notebook>().ReverseMap();
        CreateMap<NotebookModuleStyleEntity, NotebookModuleStyle>().ReverseMap();
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
