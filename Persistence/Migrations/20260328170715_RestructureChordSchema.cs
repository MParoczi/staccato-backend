using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RestructureChordSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1 — Add new columns with temporary defaults so NOT NULL columns are valid
            migrationBuilder.AddColumn<string>(
                name: "Root",
                table: "Chords",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Quality",
                table: "Chords",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Extension",
                table: "Chords",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Alternation",
                table: "Chords",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            // Step 2 — Populate Root, Quality, Extension, and display Name from old Suffix values.
            // On a fresh database the Chords table is empty; this is a safe no-op.
            // WHERE Root = '' targets only rows that have not yet been migrated.
            migrationBuilder.Sql(@"
UPDATE Chords SET
  Root       = Name,
  Quality    = CASE Suffix
                 WHEN 'major' THEN 'Major'
                 WHEN 'minor' THEN 'Minor'
                 WHEN '7'     THEN 'Seventh'
                 WHEN 'maj7'  THEN 'Major 7'
                 WHEN 'add9'  THEN 'Major'
                 WHEN 'm7'    THEN 'Minor 7'
                 WHEN 'm7b5'  THEN 'Half-Diminished'
                 WHEN 'dim'   THEN 'Diminished'
                 WHEN 'dim7'  THEN 'Diminished 7th'
                 WHEN 'aug'   THEN 'Augmented'
                 WHEN 'sus2'  THEN 'Suspended 2nd'
                 WHEN 'sus4'  THEN 'Suspended 4th'
                 ELSE 'Major'
               END,
  Extension  = CASE Suffix WHEN 'add9' THEN 'add9' ELSE NULL END,
  Alternation = NULL,
  Name       = Name + CASE Suffix
                 WHEN 'major' THEN ''
                 WHEN 'minor' THEN 'm'
                 WHEN '7'     THEN '7'
                 WHEN 'maj7'  THEN 'maj7'
                 WHEN 'add9'  THEN 'add9'
                 WHEN 'm7'    THEN 'm7'
                 WHEN 'm7b5'  THEN 'm7b5'
                 WHEN 'dim'   THEN 'dim'
                 WHEN 'dim7'  THEN 'dim7'
                 WHEN 'aug'   THEN 'aug'
                 WHEN 'sus2'  THEN 'sus2'
                 WHEN 'sus4'  THEN 'sus4'
                 ELSE Suffix
               END
WHERE Root = ''
");

            // Step 3 — Remove the temporary defaults from Root and Quality
            migrationBuilder.AlterColumn<string>(
                name: "Quality",
                table: "Chords",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: false,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Root",
                table: "Chords",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: false,
                oldDefaultValue: "");

            // Step 4 — Drop the old Suffix column
            migrationBuilder.DropColumn(
                name: "Suffix",
                table: "Chords");

            // Step 5 — Replace the old single-column index with the new composite index
            migrationBuilder.DropIndex(
                name: "IX_Chords_InstrumentId",
                table: "Chords");

            migrationBuilder.CreateIndex(
                name: "IX_Chords_InstrumentId_Root_Quality",
                table: "Chords",
                columns: new[] { "InstrumentId", "Root", "Quality" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Chords_InstrumentId_Root_Quality",
                table: "Chords");

            migrationBuilder.DropColumn(name: "Alternation", table: "Chords");
            migrationBuilder.DropColumn(name: "Extension", table: "Chords");
            migrationBuilder.DropColumn(name: "Quality", table: "Chords");
            migrationBuilder.DropColumn(name: "Root", table: "Chords");

            migrationBuilder.AddColumn<string>(
                name: "Suffix",
                table: "Chords",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Chords_InstrumentId",
                table: "Chords",
                column: "InstrumentId");
        }
    }
}
