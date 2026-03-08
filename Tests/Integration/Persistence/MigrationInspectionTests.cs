using System.Text.RegularExpressions;

namespace Tests.Integration.Persistence;

/// <summary>
///     SC-004: Inspects the generated InitialCreate migration source to assert the
///     presence of filtered indexes, Restrict FKs, and NO ACTION for PdfExports.UserId.
///     Unique constraint violations cannot be tested against InMemory EF; this test
///     verifies them via migration source inspection.
/// </summary>
public class MigrationInspectionTests
{
    // Read the migration source file from the Persistence project directory.
    // The migration class is in the Persistence assembly; we locate the source
    // by walking up from the test binary's base directory.
    private static string GetMigrationSource()
    {
        // Locate solution root by looking for Staccato.sln
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null && !dir.GetFiles("Staccato.sln").Any())
            dir = dir.Parent;

        Assert.NotNull(dir);

        var migFile = dir!.GetFiles("*_InitialCreate.cs", SearchOption.AllDirectories)
            .FirstOrDefault(f => f.FullName.Contains("Persistence") &&
                                 f.FullName.Contains("Migrations") &&
                                 !f.FullName.Contains("obj"));

        Assert.NotNull(migFile);
        return File.ReadAllText(migFile!.FullName);
    }

    // ── (a) GoogleId filtered unique index ────────────────────────────────────

    [Fact]
    public void Migration_Contains_GoogleId_FilteredUniqueIndex()
    {
        var src = GetMigrationSource();
        Assert.Contains("[GoogleId] IS NOT NULL", src);
    }

    // ── (b) PdfExports active-export partial unique index ─────────────────────

    [Fact]
    public void Migration_Contains_PdfExports_ActiveExport_PartialUniqueIndex()
    {
        var src = GetMigrationSource();
        Assert.Contains("[Status] IN (0, 1)", src);
    }

    // ── (c) Restrict appears exactly twice ────────────────────────────────────

    [Fact]
    public void Migration_Contains_Restrict_ExactlyTwice()
    {
        var src = GetMigrationSource();
        var matches = Regex.Matches(src, @"ReferentialAction\.Restrict");
        Assert.Equal(2, matches.Count);
    }

    // ── (d) PdfExports.UserId FK has NO ACTION (no onDelete clause) ───────────

    [Fact]
    public void Migration_PdfExports_UserId_FK_HasNoOnDeleteClause()
    {
        var src = GetMigrationSource();

        // Find the FK_PdfExports_Users_UserId block and assert there is no
        // onDelete: argument within it (ClientCascade generates NO ACTION).
        var fkPattern = new Regex(
            @"name:\s*""FK_PdfExports_Users_UserId"".*?}\);",
            RegexOptions.Singleline);

        var match = fkPattern.Match(src);
        Assert.True(match.Success, "FK_PdfExports_Users_UserId block not found in migration.");
        Assert.DoesNotContain("onDelete:", match.Value);
    }

    // ── (e) Sanity: all 12 CreateTable calls are present ─────────────────────

    [Fact]
    public void Migration_Contains_TwelveCreateTableCalls()
    {
        var src = GetMigrationSource();
        var count = Regex.Matches(src, @"migrationBuilder\.CreateTable\(").Count;
        Assert.Equal(12, count);
    }

    // ── (f) Both Restrict FKs are for Instrument references ──────────────────

    [Fact]
    public void Migration_BothRestrictFKs_AreForInstrumentId()
    {
        var src = GetMigrationSource();

        // Each Restrict FK should be inside a block referencing Instruments
        var restrictPattern = new Regex(
            @"onDelete:\s*ReferentialAction\.Restrict",
            RegexOptions.Singleline);

        var matches = restrictPattern.Matches(src);
        Assert.Equal(2, matches.Count);

        // Walk back 500 chars from each match to find "Instruments" nearby
        foreach (Match match in matches)
        {
            var start = Math.Max(0, match.Index - 500);
            var context = src.Substring(start, match.Index - start + match.Length);
            Assert.Contains("Instruments", context);
        }
    }
}