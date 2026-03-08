using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Persistence.Seed;

namespace Tests.Unit.Persistence;

public class SystemStylePresetSeederTests
{
    private static AppDbContext CreateContext()
    {
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SeedAsync_EmptyTable_InsertsExactlyFivePresets()
    {
        await using var ctx = CreateContext();
        var seeder = new SystemStylePresetSeeder(ctx);

        await seeder.SeedAsync();

        Assert.Equal(5, await ctx.SystemStylePresets.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_EmptyTable_ClassicIsDefault()
    {
        await using var ctx = CreateContext();
        var seeder = new SystemStylePresetSeeder(ctx);

        await seeder.SeedAsync();

        var classic = await ctx.SystemStylePresets.SingleAsync(p => p.Name == "Classic");
        Assert.True(classic.IsDefault);
    }

    [Fact]
    public async Task SeedAsync_EmptyTable_OnlyClassicIsDefault()
    {
        await using var ctx = CreateContext();
        var seeder = new SystemStylePresetSeeder(ctx);

        await seeder.SeedAsync();

        var defaultCount = await ctx.SystemStylePresets.CountAsync(p => p.IsDefault);
        Assert.Equal(1, defaultCount);
    }

    [Fact]
    public async Task SeedAsync_EmptyTable_DisplayOrdersAreOneToFiveDistinct()
    {
        await using var ctx = CreateContext();
        var seeder = new SystemStylePresetSeeder(ctx);

        await seeder.SeedAsync();

        var orders = await ctx.SystemStylePresets
            .Select(p => p.DisplayOrder)
            .OrderBy(o => o)
            .ToListAsync();

        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, orders);
    }

    [Theory]
    [InlineData("Classic", 1)]
    [InlineData("Colorful", 2)]
    [InlineData("Dark", 3)]
    [InlineData("Minimal", 4)]
    [InlineData("Pastel", 5)]
    public async Task SeedAsync_EmptyTable_EachPresetHasCorrectDisplayOrder(
        string name, int expectedOrder)
    {
        await using var ctx = CreateContext();
        var seeder = new SystemStylePresetSeeder(ctx);

        await seeder.SeedAsync();

        var preset = await ctx.SystemStylePresets.SingleAsync(p => p.Name == name);
        Assert.Equal(expectedOrder, preset.DisplayOrder);
    }

    [Theory]
    [InlineData("Classic")]
    [InlineData("Colorful")]
    [InlineData("Dark")]
    [InlineData("Minimal")]
    [InlineData("Pastel")]
    public async Task SeedAsync_EmptyTable_EachPresetStylesJsonDeserializesToArrayOfTwelve(
        string presetName)
    {
        await using var ctx = CreateContext();
        var seeder = new SystemStylePresetSeeder(ctx);

        await seeder.SeedAsync();

        var preset = await ctx.SystemStylePresets.SingleAsync(p => p.Name == presetName);
        using var doc = JsonDocument.Parse(preset.StylesJson);

        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(12, doc.RootElement.GetArrayLength());
    }

    // ── Colorful exact hex values (FR-031) ────────────────────────────────────

    [Theory]
    [InlineData("Theory", "backgroundColor", "#E0F7FA")]
    [InlineData("Theory", "headerBgColor", "#00838F")]
    [InlineData("Theory", "borderColor", "#00838F")]
    [InlineData("Practice", "backgroundColor", "#FFF3E0")]
    [InlineData("Practice", "headerBgColor", "#E65100")]
    [InlineData("Example", "backgroundColor", "#E8F5E9")]
    [InlineData("Example", "headerBgColor", "#2E7D32")]
    [InlineData("Important", "backgroundColor", "#FFFDE7")]
    [InlineData("Important", "headerBgColor", "#F57F17")]
    [InlineData("Tip", "backgroundColor", "#E3F2FD")]
    [InlineData("Tip", "headerBgColor", "#1565C0")]
    [InlineData("Homework", "backgroundColor", "#F3E5F5")]
    [InlineData("Homework", "headerBgColor", "#6A1B9A")]
    [InlineData("Question", "backgroundColor", "#FCE4EC")]
    [InlineData("Question", "headerBgColor", "#880E4F")]
    [InlineData("ChordTablature", "backgroundColor", "#F5F5F5")]
    [InlineData("ChordTablature", "headerBgColor", "#424242")]
    [InlineData("FreeText", "backgroundColor", "#FFFFFF")]
    [InlineData("FreeText", "borderColor", "#9E9E9E")]
    public async Task SeedAsync_ColorfulPreset_HasExactFR031HexValues(
        string moduleType, string field, string expectedHex)
    {
        await using var ctx = CreateContext();
        var seeder = new SystemStylePresetSeeder(ctx);

        await seeder.SeedAsync();

        var colorful = await ctx.SystemStylePresets.SingleAsync(p => p.Name == "Colorful");
        using var doc = JsonDocument.Parse(colorful.StylesJson);

        var styleObj = doc.RootElement.EnumerateArray()
            .Single(e => e.GetProperty("moduleType").GetString() == moduleType);

        Assert.Equal(expectedHex, styleObj.GetProperty(field).GetString());
    }

    // ── Colorful Title/Subtitle/Breadcrumb neutral values (FR-031) ───────────

    [Theory]
    [InlineData("Title")]
    [InlineData("Subtitle")]
    [InlineData("Breadcrumb")]
    public async Task SeedAsync_ColorfulPreset_TitleSubtitleBreadcrumb_HasWhiteBackground(
        string moduleType)
    {
        await using var ctx = CreateContext();
        var seeder = new SystemStylePresetSeeder(ctx);

        await seeder.SeedAsync();

        var colorful = await ctx.SystemStylePresets.SingleAsync(p => p.Name == "Colorful");
        using var doc = JsonDocument.Parse(colorful.StylesJson);

        var styleObj = doc.RootElement.EnumerateArray()
            .Single(e => e.GetProperty("moduleType").GetString() == moduleType);

        Assert.Equal("#FFFFFF", styleObj.GetProperty("backgroundColor").GetString());
        Assert.Equal("#212121", styleObj.GetProperty("bodyTextColor").GetString());
    }

    // ── Idempotency ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SeedAsync_NonEmptyTable_ExitsWithoutInsertingMoreRows()
    {
        await using var ctx = CreateContext();
        var seeder = new SystemStylePresetSeeder(ctx);

        await seeder.SeedAsync(); // first run
        var countAfterFirst = await ctx.SystemStylePresets.CountAsync();

        await seeder.SeedAsync(); // second run — no-op
        Assert.Equal(countAfterFirst, await ctx.SystemStylePresets.CountAsync());
    }
}