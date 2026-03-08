# Research: EF Core Entity Models and Database Persistence

**Feature**: `003-ef-core-persistence`
**Date**: 2026-03-07

---

## Decision 1: Filtered and Partial Unique Indexes in EF Core 10

**Question**: How to implement (a) a filtered unique index on `UserEntity.GoogleId` (non-null rows only) and (b) a partial unique index on `PdfExportEntity.NotebookId` (active exports only) using EF Core Fluent API?

**Decision**: Use `.HasIndex(...).IsUnique().HasFilter("SQL expression")` — natively supported in EF Core 6+, available in EF Core 10.

**Rationale**: `HasFilter` accepts a raw SQL `WHERE` clause string, generating a SQL Server filtered index in the migration without any additional raw SQL or `HasAnnotation` workarounds. This is the standard EF Core approach and produces clean, reviewable migration code.

**GoogleId filtered index**:
```csharp
entity.HasIndex(u => u.GoogleId)
    .IsUnique()
    .HasFilter("[GoogleId] IS NOT NULL");
```

**PdfExport partial index** (Pending=0, Processing=1 per ExportStatus enum):
```csharp
entity.HasIndex(e => e.NotebookId)
    .IsUnique()
    .HasFilter("[Status] = 0 OR [Status] = 1");
```

**Alternatives considered**:
- Raw `migrationBuilder.Sql(...)` in migration file — rejected because it escapes EF model management and is not reflected in `GetCurrentSnapshot()`.
- EF Core `HasAnnotation` with SQL Server-specific keys — rejected as fragile and undocumented API.

---

## Decision 2: Guitar Chord JSON File Format (`guitar_chords.json`)

**Question**: What structure should `guitar_chords.json` use so the `ChordSeeder` can deserialize it directly into `ChordEntity` records?

**Decision**: A top-level JSON array of chord descriptor objects. Each object maps directly to one `ChordEntity` row.

**Schema**:
```json
[
  {
    "name": "A",
    "suffix": "major",
    "positions": [
      {
        "label": "Open position",
        "baseFret": 1,
        "barre": null,
        "strings": [
          { "string": 1, "state": "open",    "fret": null, "finger": null },
          { "string": 2, "state": "fretted", "fret": 2,    "finger": 1   },
          { "string": 3, "state": "fretted", "fret": 2,    "finger": 2   },
          { "string": 4, "state": "fretted", "fret": 2,    "finger": 3   },
          { "string": 5, "state": "open",    "fret": null, "finger": null },
          { "string": 6, "state": "open",    "fret": null, "finger": null }
        ]
      }
    ]
  }
]
```

Field definitions:
- `name` → `ChordEntity.Name` (root note: A, Bb, B, C, C#, D, Eb, E, F, F#, G, Ab)
- `suffix` → `ChordEntity.Suffix` (quality: major, minor, 7, maj7, m7, dim, aug, sus2, sus4, m7b5, dim7, add9, 9, maj9, m9, 6, m6)
- `positions` → serialized as `ChordEntity.PositionsJson` (JSON array of ChordPosition objects)
- `barre`: `{ "fret": N, "fromString": N, "toString": N }` or `null`
- `strings`: 6 entries per chord, `string` numbers 1–6 (1 = highest pitched / thinnest string)
- `state`: `"open"` | `"fretted"` | `"muted"`

**Seeder deserialization class** (`ChordSeedRecord`):
```csharp
record ChordSeedRecord(string Name, string Suffix, JsonElement Positions);
```
`Positions` is kept as `JsonElement` and serialized back to string for `PositionsJson`.

**Coverage**: 12 root notes × 17 qualities = 204 chord entries minimum, 2–3 positions each.

**Rationale**: Flat array matches 1-to-1 with ChordEntity rows. Keeping `positions` as a raw `JsonElement` avoids defining a full C# object graph for deserialization just for seeding.

**Alternatives considered**:
- Nested structure grouped by root note (`{ "A": [ ... ], "Bb": [ ... ] }`) — rejected because it adds a mapping step and diverges from the row-per-chord entity model.
- Using chords-db npm package format — rejected as it introduces an external dependency.

---

## Decision 3: StylesJson Shape per Entity

**Question**: What is the exact JSON shape stored in each `StylesJson` property, given that `NotebookModuleStyleEntity`, `SystemStylePresetEntity`, and `UserSavedPresetEntity` all have a `StylesJson` field but represent different scopes?

**Decision**: Two distinct shapes, confirmed from frontend documentation contract:

**Shape A — Single module-type style** (used by `NotebookModuleStyleEntity.StylesJson`):
```json
{
  "backgroundColor": "#FFFFFF",
  "borderColor": "#E0E0E0",
  "borderStyle": "Solid",
  "borderWidth": 1,
  "borderRadius": 4,
  "headerBgColor": "#F5F5F5",
  "headerTextColor": "#212121",
  "bodyTextColor": "#212121",
  "fontFamily": "Default"
}
```
The `moduleType` discriminator is not included here — it is stored in the separate `ModuleType` column on the entity.

**Shape B — All 12 module-type styles** (used by `SystemStylePresetEntity.StylesJson` and `UserSavedPresetEntity.StylesJson`):
```json
[
  {
    "moduleType": "Title",
    "backgroundColor": "#FFFFFF",
    "borderColor": "#E0E0E0",
    "borderStyle": "Solid",
    "borderWidth": 1,
    "borderRadius": 4,
    "headerBgColor": "#F5F5F5",
    "headerTextColor": "#212121",
    "bodyTextColor": "#212121",
    "fontFamily": "Default"
  },
  ... (11 more entries)
]
```

**Title and Subtitle modules**: `backgroundColor`, `borderColor`, `borderStyle`, `borderWidth`, `borderRadius`, `headerBgColor`, `headerTextColor` are present in the stored JSON but the frontend ignores them for these types — only `bodyTextColor` and `fontFamily` are rendered. The seeder MUST still populate all fields for consistency.

**Rationale**: Storing all 9 fields for all 12 module types (including Title/Subtitle) avoids a special-case schema and allows the API to return a uniform shape regardless of module type.

---

## Decision 4: System Style Preset Color Palettes

**Question**: What hex values should each of the 5 system presets use for their 12 module types?

**Decision**: Derive from the Colorful preset reference in the frontend documentation; extrapolate the remaining 4 presets.

### Colorful Preset (from frontend docs — reference design)
| Module Type | BackgroundColor | HeaderBgColor | HeaderTextColor | BodyTextColor | BorderColor | BorderStyle | BorderWidth | BorderRadius | FontFamily |
|---|---|---|---|---|---|---|---|---|---|
| Title | #FFFFFF | #FFFFFF | #212121 | #212121 | #E0E0E0 | None | 0 | 0 | Default |
| Breadcrumb | #FFFFFF | #FFFFFF | #212121 | #757575 | #E0E0E0 | None | 0 | 0 | Default |
| Subtitle | #FFFFFF | #FFFFFF | #212121 | #424242 | #E0E0E0 | None | 0 | 0 | Default |
| Theory | #E0F7FA | #00838F | #FFFFFF | #212121 | #00838F | Solid | 1 | 6 | Default |
| Practice | #FFF3E0 | #E65100 | #FFFFFF | #212121 | #E65100 | Solid | 1 | 6 | Default |
| Example | #E8F5E9 | #2E7D32 | #FFFFFF | #212121 | #2E7D32 | Solid | 1 | 6 | Default |
| Important | #FFFDE7 | #F57F17 | #FFFFFF | #212121 | #F57F17 | Solid | 1 | 6 | Default |
| Tip | #E3F2FD | #1565C0 | #FFFFFF | #212121 | #1565C0 | Solid | 1 | 6 | Default |
| Homework | #F3E5F5 | #6A1B9A | #FFFFFF | #212121 | #6A1B9A | Solid | 1 | 6 | Default |
| Question | #FCE4EC | #880E4F | #FFFFFF | #212121 | #880E4F | Solid | 1 | 6 | Default |
| ChordTablature | #F5F5F5 | #424242 | #FFFFFF | #212121 | #424242 | Solid | 1 | 6 | Default |
| FreeText | #FFFFFF | #9E9E9E | #FFFFFF | #212121 | #9E9E9E | Solid | 1 | 4 | Default |

### Classic Preset (warm beige/brown — traditional book aesthetic)
All modules: BackgroundColor `#FAF7F2`, BorderColor `#C8B89A`, HeaderBgColor `#8D6E63`, HeaderTextColor `#FFFFFF`, BodyTextColor `#3E2723`, BorderStyle `Solid`, BorderWidth `1`, BorderRadius `4`, FontFamily `Serif`. Title/Subtitle/Breadcrumb: BackgroundColor `#FFFFFF`, BorderStyle `None`, BorderWidth `0`.

### Dark Preset (dark backgrounds, light text)
All modules: BackgroundColor `#1E1E1E`, BorderColor `#3C3C3C`, HeaderBgColor `#2D2D2D`, HeaderTextColor `#E0E0E0`, BodyTextColor `#CFCFCF`, BorderStyle `Solid`, BorderWidth `1`, BorderRadius `6`, FontFamily `Default`. Title/Subtitle/Breadcrumb: BackgroundColor `#121212`, BodyTextColor `#FFFFFF`, BorderStyle `None`, BorderWidth `0`.

### Minimal Preset (white/near-white, thin borders)
All modules: BackgroundColor `#FFFFFF`, BorderColor `#E0E0E0`, HeaderBgColor `#FAFAFA`, HeaderTextColor `#616161`, BodyTextColor `#212121`, BorderStyle `Solid`, BorderWidth `1`, BorderRadius `2`, FontFamily `Default`. Title/Subtitle/Breadcrumb: BorderStyle `None`, BorderWidth `0`.

### Pastel Preset (soft pastel per module type)
| Module Type | BackgroundColor | HeaderBgColor |
|---|---|---|
| Theory | #E8F4F8 | #B3D9E8 |
| Practice | #FFF0E8 | #FFCBA4 |
| Example | #EBF8EB | #B2DEB2 |
| Important | #FFFBE8 | #FFE99A |
| Tip | #EBF3FF | #AECBF5 |
| Homework | #F5EBFF | #DABFFF |
| Question | #FFE8F0 | #FFAECB |
| ChordTablature | #F0F0F0 | #CCCCCC |
| FreeText | #FFFFFF | #E0E0E0 |
All pastel: HeaderTextColor `#424242`, BodyTextColor `#424242`, BorderColor matches HeaderBgColor, BorderStyle `Solid`, BorderWidth `1`, BorderRadius `8`, FontFamily `Default`. Title/Subtitle/Breadcrumb: same as Minimal.

---

## Decision 5: DbInitializer Invocation Pattern

**Question**: How should `DbInitializer` be invoked synchronously before `app.Run()` without violating the "no `IConfiguration` in services" rule?

**Decision**: Register `DbInitializer` as a scoped service in DI. After `var app = builder.Build()`, create a scope, resolve `DbInitializer`, and call it.

```csharp
// In Program.cs, after app = builder.Build():
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await initializer.InitializeAsync();
}
```

`DbInitializer` receives `AppDbContext` via constructor injection (not `IConfiguration` directly). The connection string is configured once in `AddDatabase` via `AddDbContext`.

**Rationale**: Keeps DbInitializer testable (injectable), avoids `IConfiguration` in the service itself, and uses the standard `IServiceScope` pattern for startup tasks. The `await` on `InitializeAsync` ensures all initialization is complete before `app.Run()`.

---

## Decision 6: Migration Strategy

**Question**: Should the initial migration use `EnsureCreated` or `Database.Migrate()`?

**Decision**: `DbInitializer` calls `Database.MigrateAsync()` (not `EnsureCreated`). The `InitialCreate` migration is generated once and checked in.

**Rationale**: `EnsureCreated` skips migrations entirely and bypasses the migration history table, making future schema changes impossible via EF migrations. `MigrateAsync` applies pending migrations idempotently and is correct for all environments including development.

**Note**: The `dotnet ef migrations add InitialCreate` command must be run manually after all configurations are in place (step 8 in user input). The generated migration file is verified against expected schema.

---

## Decision 7: Instrument Display Names and String Counts

| InstrumentKey | DisplayName | StringCount |
|---|---|---|
| Guitar6String | 6-String Guitar | 6 |
| Guitar7String | 7-String Guitar | 7 |
| Bass4String | 4-String Bass | 4 |
| Bass5String | 5-String Bass | 5 |
| Ukulele4String | Ukulele | 4 |
| Banjo4String | 4-String Banjo | 4 |
| Banjo5String | 5-String Banjo | 5 |
