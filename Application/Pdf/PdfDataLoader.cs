using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Interfaces.Repositories;
using DomainModels.BuildingBlocks;
using DomainModels.Enums;
using DomainModels.Models;

namespace Application.Pdf;

public class PdfDataLoader(
    IPdfExportRepository pdfExportRepo,
    INotebookRepository notebookRepo,
    IUserRepository userRepo,
    IInstrumentRepository instrumentRepo,
    INotebookModuleStyleRepository styleRepo,
    ILessonRepository lessonRepo,
    ILessonPageRepository lessonPageRepo,
    IModuleRepository moduleRepo,
    IChordRepository chordRepo)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase), new BuildingBlockConverter() }
    };

    public async Task<PdfExportData?> LoadAsync(Guid exportId, CancellationToken ct = default)
    {
        var export = await pdfExportRepo.GetByIdAsync(exportId, ct);
        if (export is null) return null;

        var notebookResult = await notebookRepo.GetWithStylesAsync(export.NotebookId, ct);
        if (notebookResult is null) return null;

        var (notebook, notebookStyles) = notebookResult.Value;

        var user = await userRepo.GetByIdAsync(export.UserId, ct);
        if (user is null) return null;

        var instrument = await instrumentRepo.GetByIdAsync(notebook.InstrumentId, ct);

        // Load styles into dictionary
        var styles = new Dictionary<ModuleType, ModuleStyleData>();
        foreach (var style in notebookStyles)
        {
            var parsed = ParseStyleJson(style.StylesJson);
            if (parsed is not null)
                styles[style.ModuleType] = parsed;
        }

        // Load lessons (all or filtered)
        var allLessons = await lessonRepo.GetByNotebookIdOrderedByCreatedAtAsync(export.NotebookId, ct);
        var lessons = export.LessonIds is not null
            ? allLessons.Where(l => export.LessonIds.Contains(l.Id)).ToList()
            : allLessons.ToList();

        // Collect all chord IDs for batch loading
        var chordIds = new HashSet<Guid>();
        var lessonDataList = new List<LessonRenderData>();

        foreach (var lesson in lessons)
        {
            var pages = await lessonPageRepo.GetByLessonIdOrderedAsync(lesson.Id, ct);
            var pageDataList = new List<PageRenderData>();

            foreach (var page in pages)
            {
                var modules = await moduleRepo.GetByPageIdAsync(page.Id, ct);
                var moduleDataList = new List<ModuleRenderData>();

                foreach (var module in modules)
                {
                    var blocks = DeserializeBuildingBlocks(module.ContentJson);
                    CollectChordIds(blocks, chordIds);

                    moduleDataList.Add(new ModuleRenderData(
                        module.ModuleType,
                        module.GridX,
                        module.GridY,
                        module.GridWidth,
                        module.GridHeight,
                        module.ZIndex,
                        blocks));
                }

                pageDataList.Add(new PageRenderData(page.PageNumber, moduleDataList));
            }

            lessonDataList.Add(new LessonRenderData(lesson.Title, pageDataList));
        }

        // Batch load referenced chords
        var chords = new Dictionary<Guid, Chord>();
        foreach (var chordId in chordIds)
        {
            var chord = await chordRepo.GetByIdAsync(chordId, ct);
            if (chord is not null)
                chords[chord.Id] = chord;
        }

        return new PdfExportData(
            NotebookTitle: notebook.Title,
            CoverColor: notebook.CoverColor,
            PageSize: notebook.PageSize,
            CreatedAt: notebook.CreatedAt,
            OwnerName: $"{user.FirstName} {user.LastName}",
            Language: user.Language,
            InstrumentName: instrument?.DisplayName ?? "",
            InstrumentStringCount: instrument?.StringCount ?? 6,
            Styles: styles,
            Lessons: lessonDataList,
            Chords: chords);
    }

    private static IReadOnlyList<BuildingBlock> DeserializeBuildingBlocks(string contentJson)
    {
        if (string.IsNullOrWhiteSpace(contentJson) || contentJson == "[]")
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<BuildingBlock>>(contentJson, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static void CollectChordIds(IReadOnlyList<BuildingBlock> blocks, HashSet<Guid> chordIds)
    {
        foreach (var block in blocks)
        {
            switch (block)
            {
                case ChordTablatureGroupBlock tab:
                    foreach (var item in tab.Items)
                        chordIds.Add(item.ChordId);
                    break;
                case ChordProgressionBlock prog:
                    foreach (var section in prog.Sections)
                    foreach (var measure in section.Measures)
                    foreach (var beat in measure.Chords)
                        chordIds.Add(beat.ChordId);
                    break;
            }
        }
    }

    private static ModuleStyleData? ParseStyleJson(string stylesJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(stylesJson);
            var root = doc.RootElement;
            return new ModuleStyleData(
                BackgroundColor: root.GetProperty("backgroundColor").GetString() ?? "#FFFFFF",
                BorderColor: root.GetProperty("borderColor").GetString() ?? "#000000",
                BorderWidth: root.GetProperty("borderWidth").GetInt32(),
                BorderRadius: root.GetProperty("borderRadius").GetInt32(),
                HeaderBgColor: root.GetProperty("headerBgColor").GetString() ?? "#EEEEEE",
                HeaderTextColor: root.GetProperty("headerTextColor").GetString() ?? "#000000",
                BodyTextColor: root.GetProperty("bodyTextColor").GetString() ?? "#000000",
                FontFamily: root.GetProperty("fontFamily").GetString() ?? "Arial");
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Custom converter for polymorphic BuildingBlock deserialization based on "type" discriminator.
    /// </summary>
    private sealed class BuildingBlockConverter : JsonConverter<BuildingBlock>
    {
        public override BuildingBlock? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeElement))
                return null;

            var typeString = typeElement.GetString();
            if (!Enum.TryParse<BuildingBlockType>(typeString, true, out var blockType))
                return null;

            var json = root.GetRawText();
            var innerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };

            return blockType switch
            {
                BuildingBlockType.SectionHeading => JsonSerializer.Deserialize<SectionHeadingBlock>(json, innerOptions),
                BuildingBlockType.Date => JsonSerializer.Deserialize<DateBlock>(json, innerOptions),
                BuildingBlockType.Text => JsonSerializer.Deserialize<TextBlock>(json, innerOptions),
                BuildingBlockType.BulletList => JsonSerializer.Deserialize<BulletListBlock>(json, innerOptions),
                BuildingBlockType.NumberedList => JsonSerializer.Deserialize<NumberedListBlock>(json, innerOptions),
                BuildingBlockType.CheckboxList => JsonSerializer.Deserialize<CheckboxListBlock>(json, innerOptions),
                BuildingBlockType.Table => JsonSerializer.Deserialize<TableBlock>(json, innerOptions),
                BuildingBlockType.MusicalNotes => JsonSerializer.Deserialize<MusicalNotesBlock>(json, innerOptions),
                BuildingBlockType.ChordProgression => JsonSerializer.Deserialize<ChordProgressionBlock>(json, innerOptions),
                BuildingBlockType.ChordTablatureGroup => JsonSerializer.Deserialize<ChordTablatureGroupBlock>(json, innerOptions),
                _ => null
            };
        }

        public override void Write(Utf8JsonWriter writer, BuildingBlock value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
