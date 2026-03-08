using System.Text.Json;
using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Seed;

public class SystemStylePresetSeeder(AppDbContext context)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public virtual async Task SeedAsync(CancellationToken ct = default)
    {
        if (await context.SystemStylePresets.AnyAsync(ct)) return;

        context.SystemStylePresets.AddRange(
            BuildPreset("Classic",        1, isDefault: true,  BuildClassicStyles()),
            BuildPreset("Colorful",       2, isDefault: false, BuildColorfulStyles()),
            BuildPreset("Dark",           3, isDefault: false, BuildDarkStyles()),
            BuildPreset("Minimal",        4, isDefault: false, BuildMinimalStyles()),
            BuildPreset("Pastel",         5, isDefault: false, BuildPastelStyles())
        );

        await context.SaveChangesAsync(ct);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static SystemStylePresetEntity BuildPreset(
        string name, int displayOrder, bool isDefault, IReadOnlyList<object> styles)
    {
        return new SystemStylePresetEntity
        {
            Id           = Guid.NewGuid(),
            Name         = name,
            DisplayOrder = displayOrder,
            IsDefault    = isDefault,
            StylesJson   = JsonSerializer.Serialize(styles, JsonOptions)
        };
    }

    private static object Style(
        string moduleType,
        string backgroundColor, string borderColor,
        string borderStyle, int borderWidth, int borderRadius,
        string headerBgColor, string headerTextColor,
        string bodyTextColor, string fontFamily)
        => new
        {
            moduleType,
            backgroundColor,
            borderColor,
            borderStyle,
            borderWidth,
            borderRadius,
            headerBgColor,
            headerTextColor,
            bodyTextColor,
            fontFamily
        };

    // ── Classic (warm beige/brown, Serif) ────────────────────────────────────

    private static IReadOnlyList<object> BuildClassicStyles() =>
    [
        Style("Title",          "#FDFAF4", "#C9A84C", "Solid", 2, 2, "#8B6914", "#FFFDE7", "#2C1810", "Serif"),
        Style("Breadcrumb",     "#FDFAF4", "#C9A84C", "Solid", 1, 2, "#9B7B3A", "#FFFDE7", "#2C1810", "Serif"),
        Style("Subtitle",       "#FDFAF4", "#C9A84C", "Solid", 1, 2, "#8B6914", "#FFFDE7", "#2C1810", "Serif"),
        Style("Theory",         "#FAF7EE", "#C9A84C", "Solid", 1, 3, "#8B6914", "#FFFDE7", "#2C1810", "Serif"),
        Style("Practice",       "#FAF7EE", "#C9A84C", "Solid", 1, 3, "#7B5E28", "#FFFDE7", "#2C1810", "Serif"),
        Style("Example",        "#FAF7EE", "#C9A84C", "Solid", 1, 3, "#8B6914", "#FFFDE7", "#2C1810", "Serif"),
        Style("Important",      "#FFFBF0", "#D4A84B", "Solid", 2, 3, "#A0782A", "#FFFDE7", "#2C1810", "Serif"),
        Style("Tip",            "#FDFAF4", "#C9A84C", "Solid", 1, 3, "#7B5E28", "#FFFDE7", "#2C1810", "Serif"),
        Style("Homework",       "#FAF7EE", "#C9A84C", "Solid", 1, 3, "#8B6914", "#FFFDE7", "#2C1810", "Serif"),
        Style("Question",       "#FFFBF0", "#D4A84B", "Solid", 1, 3, "#9B7B3A", "#FFFDE7", "#2C1810", "Serif"),
        Style("ChordTablature", "#FDFAF4", "#C9A84C", "Solid", 1, 3, "#7B5E28", "#FFFDE7", "#2C1810", "Serif"),
        Style("FreeText",       "#FFFFFF", "#C9A84C", "Solid", 1, 3, "#FDFAF4", "#2C1810", "#2C1810", "Serif"),
    ];

    // ── Colorful (exact FR-031 hex values) ───────────────────────────────────

    private static IReadOnlyList<object> BuildColorfulStyles() =>
    [
        Style("Title",          "#FFFFFF", "#E0E0E0", "Solid", 1, 0, "#FFFFFF", "#212121", "#212121", "Default"),
        Style("Breadcrumb",     "#FFFFFF", "#E0E0E0", "Solid", 1, 0, "#FFFFFF", "#212121", "#212121", "Default"),
        Style("Subtitle",       "#FFFFFF", "#E0E0E0", "Solid", 1, 0, "#FFFFFF", "#212121", "#212121", "Default"),
        Style("Theory",         "#E0F7FA", "#00838F", "Solid", 1, 4, "#00838F", "#FFFFFF", "#212121", "Default"),
        Style("Practice",       "#FFF3E0", "#E65100", "Solid", 1, 4, "#E65100", "#FFFFFF", "#212121", "Default"),
        Style("Example",        "#E8F5E9", "#2E7D32", "Solid", 1, 4, "#2E7D32", "#FFFFFF", "#212121", "Default"),
        Style("Important",      "#FFFDE7", "#F57F17", "Solid", 1, 4, "#F57F17", "#FFFFFF", "#212121", "Default"),
        Style("Tip",            "#E3F2FD", "#1565C0", "Solid", 1, 4, "#1565C0", "#FFFFFF", "#212121", "Default"),
        Style("Homework",       "#F3E5F5", "#6A1B9A", "Solid", 1, 4, "#6A1B9A", "#FFFFFF", "#212121", "Default"),
        Style("Question",       "#FCE4EC", "#880E4F", "Solid", 1, 4, "#880E4F", "#FFFFFF", "#212121", "Default"),
        Style("ChordTablature", "#F5F5F5", "#424242", "Solid", 1, 4, "#424242", "#FFFFFF", "#212121", "Default"),
        Style("FreeText",       "#FFFFFF", "#9E9E9E", "Solid", 1, 4, "#FFFFFF", "#9E9E9E", "#212121", "Default"),
    ];

    // ── Dark (dark backgrounds, light text) ──────────────────────────────────

    private static IReadOnlyList<object> BuildDarkStyles() =>
    [
        Style("Title",          "#1E1E1E", "#3A3A3A", "Solid", 2, 3, "#252525", "#E0E0E0", "#C8C8C8", "Default"),
        Style("Breadcrumb",     "#1E1E1E", "#3A3A3A", "Solid", 1, 3, "#252525", "#E0E0E0", "#C8C8C8", "Default"),
        Style("Subtitle",       "#1E1E1E", "#3A3A3A", "Solid", 1, 3, "#252525", "#E0E0E0", "#C8C8C8", "Default"),
        Style("Theory",         "#1A2332", "#3D5A80", "Solid", 1, 4, "#22354A", "#98C1D9", "#C8C8C8", "Default"),
        Style("Practice",       "#1E1E1E", "#4A3F35", "Solid", 1, 4, "#2A2520", "#D4A96A", "#C8C8C8", "Default"),
        Style("Example",        "#1A2B1A", "#3A6B3A", "Solid", 1, 4, "#223322", "#7DC87D", "#C8C8C8", "Default"),
        Style("Important",      "#2B2400", "#6B5E00", "Solid", 1, 4, "#3A3200", "#D4A84B", "#C8C8C8", "Default"),
        Style("Tip",            "#1A2332", "#2E5B8A", "Solid", 1, 4, "#1E3550", "#7EB8D4", "#C8C8C8", "Default"),
        Style("Homework",       "#221A2B", "#5A3A7A", "Solid", 1, 4, "#2D2238", "#A87DC8", "#C8C8C8", "Default"),
        Style("Question",       "#2B1A1A", "#7A3A3A", "Solid", 1, 4, "#3A2222", "#C87D7D", "#C8C8C8", "Default"),
        Style("ChordTablature", "#1E1E1E", "#444444", "Solid", 1, 4, "#2A2A2A", "#888888", "#C8C8C8", "Monospace"),
        Style("FreeText",       "#1E1E1E", "#3A3A3A", "Solid", 1, 4, "#252525", "#C8C8C8", "#C8C8C8", "Default"),
    ];

    // ── Minimal (white/near-white, thin borders, no vivid headers) ───────────

    private static IReadOnlyList<object> BuildMinimalStyles() =>
    [
        Style("Title",          "#FFFFFF", "#E0E0E0", "Solid", 1, 0, "#FFFFFF", "#424242", "#212121", "Default"),
        Style("Breadcrumb",     "#FFFFFF", "#E0E0E0", "Solid", 1, 0, "#FFFFFF", "#424242", "#212121", "Default"),
        Style("Subtitle",       "#FFFFFF", "#E0E0E0", "Solid", 1, 0, "#FFFFFF", "#424242", "#212121", "Default"),
        Style("Theory",         "#FFFFFF", "#BDBDBD", "Solid", 1, 0, "#FAFAFA", "#424242", "#212121", "Default"),
        Style("Practice",       "#FFFFFF", "#BDBDBD", "Solid", 1, 0, "#FAFAFA", "#424242", "#212121", "Default"),
        Style("Example",        "#FFFFFF", "#BDBDBD", "Solid", 1, 0, "#FAFAFA", "#424242", "#212121", "Default"),
        Style("Important",      "#FFFFFF", "#BDBDBD", "Solid", 2, 0, "#FAFAFA", "#424242", "#212121", "Default"),
        Style("Tip",            "#FFFFFF", "#BDBDBD", "Solid", 1, 0, "#FAFAFA", "#424242", "#212121", "Default"),
        Style("Homework",       "#FFFFFF", "#BDBDBD", "Solid", 1, 0, "#FAFAFA", "#424242", "#212121", "Default"),
        Style("Question",       "#FFFFFF", "#BDBDBD", "Solid", 1, 0, "#FAFAFA", "#424242", "#212121", "Default"),
        Style("ChordTablature", "#FFFFFF", "#BDBDBD", "Solid", 1, 0, "#FAFAFA", "#424242", "#212121", "Default"),
        Style("FreeText",       "#FFFFFF", "#E0E0E0", "Solid", 1, 0, "#FFFFFF", "#424242", "#212121", "Default"),
    ];

    // ── Pastel (soft muted backgrounds per module type) ───────────────────────

    private static IReadOnlyList<object> BuildPastelStyles() =>
    [
        Style("Title",          "#E8F4FD", "#AED6F1", "Solid", 1, 4, "#AED6F1", "#1A5276", "#1A5276", "Default"),
        Style("Breadcrumb",     "#FEF9E7", "#F7DC6F", "Solid", 1, 4, "#F7DC6F", "#7D6608", "#7D6608", "Default"),
        Style("Subtitle",       "#E9F7EF", "#82E0AA", "Solid", 1, 4, "#82E0AA", "#1E8449", "#1E8449", "Default"),
        Style("Theory",         "#FFFDE7", "#F9E79F", "Solid", 1, 4, "#F9E79F", "#7D6608", "#333333", "Default"),
        Style("Practice",       "#F4ECF7", "#C39BD3", "Solid", 1, 4, "#C39BD3", "#4A235A", "#333333", "Default"),
        Style("Example",        "#E8F8F5", "#76D7C4", "Solid", 1, 4, "#76D7C4", "#117A65", "#333333", "Default"),
        Style("Important",      "#FEF5E7", "#FAD7A0", "Solid", 1, 4, "#FAD7A0", "#784212", "#333333", "Default"),
        Style("Tip",            "#EBF5FB", "#85C1E9", "Solid", 1, 4, "#85C1E9", "#154360", "#333333", "Default"),
        Style("Homework",       "#F4ECF7", "#BB8FCE", "Solid", 1, 4, "#BB8FCE", "#5B2C6F", "#333333", "Default"),
        Style("Question",       "#FDEDEC", "#F1948A", "Solid", 1, 4, "#F1948A", "#7B241C", "#333333", "Default"),
        Style("ChordTablature", "#EAF2FF", "#A9CCE3", "Solid", 1, 4, "#A9CCE3", "#1B4F72", "#333333", "Default"),
        Style("FreeText",       "#FFFFFF", "#D5D8DC", "Solid", 1, 4, "#F7F9F9", "#566573", "#333333", "Default"),
    ];
}
