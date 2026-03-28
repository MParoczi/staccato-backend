using System.Text.Json;
using System.Text.Json.Serialization;
using ApiModels.Chords;
using ApiModels.Instruments;
using ApiModels.Notebooks;
using ApiModels.Users;
using AutoMapper;
using DomainModels.Enums;
using DomainModels.Models;

namespace Api.Mapping;

public class DomainToResponseProfile : Profile
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private record StyleProperties(
        string BackgroundColor,
        string BorderColor,
        string BorderStyle,
        int BorderWidth,
        int BorderRadius,
        string HeaderBgColor,
        string HeaderTextColor,
        string BodyTextColor,
        string FontFamily);

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
                JsonSerializer.Deserialize<List<StyleEntryDto>>(s.StylesJson)
                ?? new List<StyleEntryDto>()));

        CreateMap<Instrument, InstrumentResponse>()
            .ConstructUsing((s, _) => new InstrumentResponse(s.Id, s.Key.ToString(), s.DisplayName, s.StringCount));

        CreateMap<ChordBarre, ChordBarreResponse>()
            .ConstructUsing((s, _) => new ChordBarreResponse(s.Fret, s.FromString, s.StringTo));

        CreateMap<ChordString, ChordStringResponse>()
            .ConstructUsing((s, _) => new ChordStringResponse(
                s.StringNumber,
                s.State.ToString().ToLower(),
                s.Fret,
                s.Finger))
            .ForMember(dest => dest.State, opt => opt.Ignore())
            .ForMember(dest => dest.String, opt => opt.Ignore())
            .ForMember(dest => dest.Fret, opt => opt.Ignore())
            .ForMember(dest => dest.Finger, opt => opt.Ignore());

        CreateMap<ChordPosition, ChordPositionResponse>()
            .ConstructUsing((s, ctx) => new ChordPositionResponse(
                s.Label,
                s.BaseFret,
                s.Barre != null ? ctx.Mapper.Map<ChordBarreResponse>(s.Barre) : null,
                ctx.Mapper.Map<IReadOnlyList<ChordStringResponse>>(s.Strings)));

        CreateMap<Chord, ChordSummaryResponse>()
            .ConstructUsing((s, ctx) => new ChordSummaryResponse(
                s.Id,
                s.InstrumentKey.ToString(),
                s.Name,
                s.Root,
                s.Quality,
                s.Extension,
                s.Alternation,
                ctx.Mapper.Map<ChordPositionResponse>(s.Positions[0])));

        CreateMap<Chord, ChordDetailResponse>()
            .ConstructUsing((s, ctx) => new ChordDetailResponse(
                s.Id,
                s.InstrumentKey.ToString(),
                s.Name,
                s.Root,
                s.Quality,
                s.Extension,
                s.Alternation,
                ctx.Mapper.Map<IReadOnlyList<ChordPositionResponse>>(s.Positions)));

        CreateMap<NotebookModuleStyle, ModuleStyleResponse>()
            .ConvertUsing((src, _, _) =>
            {
                var props = JsonSerializer.Deserialize<StyleProperties>(src.StylesJson, JsonOptions)!;
                return new ModuleStyleResponse(
                    src.Id,
                    src.NotebookId,
                    src.ModuleType.ToString(),
                    props.BackgroundColor,
                    props.BorderColor,
                    props.BorderStyle,
                    props.BorderWidth,
                    props.BorderRadius,
                    props.HeaderBgColor,
                    props.HeaderTextColor,
                    props.BodyTextColor,
                    props.FontFamily);
            });

        CreateMap<NotebookSummary, NotebookSummaryResponse>()
            .ConstructUsing((s, _) => new NotebookSummaryResponse(
                s.Id,
                s.Title,
                s.InstrumentName,
                s.PageSize.ToString(),
                s.CoverColor,
                s.LessonCount,
                s.CreatedAt.ToString("o"),
                s.UpdatedAt.ToString("o")));
    }
}