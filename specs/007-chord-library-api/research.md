# Research: Chord Library API

**Feature**: 007-chord-library-api
**Date**: 2026-03-28

---

## 1. Codebase Audit — What Already Exists

### ✅ Complete and correct (no changes needed)
| Artefact | Location | Notes |
|---|---|---|
| `InstrumentEntity` | `EntityModels/Entities/InstrumentEntity.cs` | Has `Key`, `DisplayName`, `StringCount` |
| `InstrumentConfiguration` | `Persistence/Configurations/InstrumentConfiguration.cs` | Unique index on `Key` |
| `InstrumentSeeder` | `Persistence/Seed/InstrumentSeeder.cs` | Seeds all 7 instruments; skip-if-any |
| `IInstrumentRepository` | `Domain/Interfaces/Repositories/IInstrumentRepository.cs` | Has `GetAllAsync` |
| `InstrumentRepository` | `Repository/Repositories/InstrumentRepository.cs` | Ordered by `DisplayName` |
| `Instrument` domain model | `DomainModels/Models/Instrument.cs` | Complete |
| `ChordStringState` enum | `DomainModels/Enums/ChordStringState.cs` | `Open`, `Fretted`, `Muted` |
| `InstrumentKey` enum | `DomainModels/Enums/InstrumentKey.cs` | All 7 keys present |
| `IChordRepository` interface | `Domain/Interfaces/Repositories/IChordRepository.cs` | `SearchAsync(instrumentId, root?, quality?)` |
| `DbInitializer` | `Persistence/DbInitializer.cs` | Calls all seeders in order |
| `EntityToDomainProfile` | `Repository/Mapping/EntityToDomainProfile.cs` | Has `ChordEntity↔Chord` and `InstrumentEntity↔Instrument` |
| Repository + service DI | `Application/Extensions/ServiceCollectionExtensions.cs` | `IChordRepository` and `IInstrumentRepository` already registered |
| `guitar_chords.json` | `Persistence/Data/guitar_chords.json` | 144 entries (12 roots × 12 qualities) |

### ⚠️ Exists but needs changes
| Artefact | Gap | Required Change |
|---|---|---|
| `ChordEntity` | Missing `Root` and `Quality` columns | Add `Root`, `Quality` string properties; rename semantic meaning of `Name` to display name |
| `ChordConfiguration` | Missing `Root`/`Quality` column config and composite index | Add column configs + `HasIndex((InstrumentId, Root, Quality))` |
| `ChordSeeder` | File path access; skip-if-any; no Root/Quality mapping | Switch to embedded resource; differential additive seeding; add Root/Quality |
| `ChordRecord` (private DTO) | No `Root` or `Quality` — incorrectly named `Name` for root | Use existing `Name` as root, `Suffix` as quality in mapping |
| `ChordRepository.SearchAsync` | Filters on `c.Name == root` and `c.Suffix == quality`; no ordering; no Include | Fix to `c.Root`/`c.Quality`; add case-insensitive; add ordering; add `.Include(c => c.Instrument)` |
| `Chord` domain model | Missing `Root`, `Quality`, `InstrumentKey`, `Positions` | Add all four |
| `Persistence.csproj` | `guitar_chords.json` is `<Content CopyToOutputDirectory>` | Change to `<EmbeddedResource>` |
| `IInstrumentRepository` | No `GetByKeyAsync` method | Add `GetByKeyAsync(InstrumentKey, ct)` for service-layer lookup |
| Existing chord seeder tests | Override via `virtual ChordFilePath` path; will break when switching to embedded resource | Rework override to provide `Stream` instead of a file path |

### ❌ Does not exist (must create)
- `IChordService` / `ChordService` in `Domain/Services/`
- `IInstrumentService` / `InstrumentService` in `Domain/Services/`
- `ChordsController` / `InstrumentsController` in `Api/Controllers/`
- `ChordSummaryResponse`, `ChordDetailResponse`, `ChordPositionResponse`, `ChordBarreResponse`, `ChordStringResponse` in `ApiModels/Chords/`
- `InstrumentResponse` in `ApiModels/Instruments/`
- Chord/Instrument mappings in `Api/Mapping/DomainToResponseProfile.cs`
- `ChordPosition`, `ChordBarre`, `ChordString` domain models in `DomainModels/Models/`
- EF Core migration for `Root` and `Quality` columns
- Unit tests: `ChordServiceTests`, `InstrumentServiceTests`
- Integration tests: `ChordsControllerTests`, `InstrumentsControllerTests`

---

## 2. guitar_chords.json Format

**Location**: `Persistence/Data/guitar_chords.json`
**Encoding**: UTF-8 with BOM — seeder must strip BOM when reading via `StreamReader`
**Size**: 144 entries

**Top-level JSON structure** (flat array):
```json
[
  {
    "name": "A",
    "suffix": "major",
    "positions": [
      {
        "label": "1",
        "baseFret": 5,
        "barre": { "fret": 5, "fromString": 1, "toString": 6 },
        "strings": [
          { "string": 6, "state": "fretted", "fret": 5, "finger": 1 },
          ...
        ]
      }
    ]
  },
  ...
]
```

**Field mapping to entity**:
| JSON field | Maps to `ChordEntity` | Notes |
|---|---|---|
| `name` | `Root` | Root note letter ("A", "F#", "Bb") |
| `suffix` | `Quality` | Quality string ("major", "minor", "maj7", "dim", etc.) |
| derived `name + " " + suffix` | `Name` | Display name ("A major") |
| `suffix` | `Suffix` | Same as Quality (kept for API response `suffix` field) |
| serialized `positions` | `PositionsJson` | Stored as camelCase JSON array |

**Distinct suffixes** (qualities): major, minor, 7, maj7, m7, dim, aug, sus2, sus4, m7b5, and 2 more

---

## 3. Key Design Decisions

### D1 — `ChordEntity.Name` semantic change
- **Before**: stored the root note letter ("A", "F#")
- **After**: stores the human-readable display name ("A major", "F# dim")
- **Impact**: existing `ChordSeederHappyPathTests` asserts `chord.Name == "C"` — must be updated to assert `chord.Name == "C major"` (or whatever the quality is)

### D2 — Embedded resource, not file copy
- `guitar_chords.json` must be `<EmbeddedResource>` in `Persistence.csproj`
- Read via `typeof(ChordSeeder).Assembly.GetManifestResourceStream("Persistence.Data.guitar_chords.json")`
- Seeder override mechanism changes from `virtual string ChordFilePath` → `virtual Stream? GetChordStream()` (returns `null` to signal "use embedded")
- Test subclass overrides `GetChordStream()` to return a `MemoryStream` from a byte array, removing the temp-file dependency

### D3 — Differential additive seeding (per spec clarification)
- Natural key for `Instrument`: `Key` enum value
- Natural key for `Chord`: `(InstrumentId, Root, Quality, Suffix)`
- Both seeders query existing records once, build a HashSet of existing keys, insert only missing ones

### D4 — `InstrumentKey` on `Chord` domain model (denormalized)
- Required because `ChordSummary` and `ChordDetail` responses include `instrumentKey`
- `ChordRepository.SearchAsync` uses `.Include(c => c.Instrument)` to load the navigation property
- `ChordRepository.GetByIdAsync` is overridden to also `.Include(c => c.Instrument)` (base class doesn't include)
- AutoMapper maps `ChordEntity.Instrument.Key → Chord.InstrumentKey`

### D5 — `Positions` deserialized in Repository mapping
- `Chord` domain model gains `Positions: List<ChordPosition>` (not the raw JSON string)
- `EntityToDomainProfile` maps `ChordEntity.PositionsJson → Chord.Positions` via `JsonSerializer.Deserialize`
- `Chord.PositionsJson` field is **removed** from the domain model (only needed for entity layer)
- `ChordService` uses `Chord.Positions[0]` as the preview position for list results

### D6 — Service-layer validation of instrument existence
- `ChordService.SearchAsync` calls `IInstrumentRepository.GetByKeyAsync(key, ct)` before querying chords
- If no instrument found: throws `NotFoundException` with code `INSTRUMENT_NOT_FOUND`
- `BadRequestException` is NOT thrown here because enum binding already rejects unknown key strings

### D7 — `GetByKeyAsync` on `IInstrumentRepository`
- New method added to `IInstrumentRepository` (extends the interface minimally)
- `InstrumentRepository` implements it with a simple `.Where(i => i.Key == key).FirstOrDefaultAsync(ct)`

### D8 — Response caching (plan-level addition beyond spec)
- User-specified: `GET /chords` responses cached 5 minutes
- Implementation: `services.AddResponseCaching()` + `app.UseResponseCaching()` (before `UseAuthentication`)
- Controller attribute: `[ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]`
- Also applied to `GET /chords/{id}` (same 5-minute TTL; chord data never changes)
- `GET /instruments` does NOT need caching attribute (rarely called, but could be added)
- No new library — ASP.NET Core built-in

### D9 — Case-insensitive filtering
- SQL Server `nvarchar` comparisons are collation-dependent; to guarantee case-insensitive:
  use EF Core `.Where(c => c.Root.ToLower() == root.ToLower())` pattern
  (EF translates to `LOWER()` in SQL, which is collation-independent)

### D10 — Ordering
- `GET /chords` results ordered by `Root` ascending, then `Quality` ascending (per spec clarification)
- `GET /instruments` results ordered by `DisplayName` ascending (already implemented)

---

## 4. Migration Strategy

The EF Core migration must:
1. Add `Root nvarchar(200) NOT NULL DEFAULT ''`
2. Add `Quality nvarchar(200) NOT NULL DEFAULT ''`
3. Run data population SQL: `UPDATE Chords SET Root = Name, Quality = Suffix, Name = Name + ' ' + Suffix`
4. Remove the default constraints (so columns are truly required)
5. Add composite index `IX_Chords_InstrumentId_Root_Quality`

Step 3 uses `migrationBuilder.Sql()` — the migration is not purely scaffolded.

> **Note**: If the Chords table is empty at migration time (fresh DB), step 3 is a no-op and the differential seeder will populate all data correctly on startup.

---

## 5. No-NEEDS-CLARIFICATION Summary

All spec requirements are implementable with the existing stack. No new libraries required. Response caching (D8) uses ASP.NET Core built-in middleware.
