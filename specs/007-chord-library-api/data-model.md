# Data Model: Chord Library API

**Feature**: 007-chord-library-api
**Date**: 2026-03-28

---

## Entity Changes

### ChordEntity *(modify)*

**File**: `EntityModels/Entities/ChordEntity.cs`

| Property | Type | Change | Notes |
|---|---|---|---|
| `Id` | `Guid` | Unchanged | PK |
| `InstrumentId` | `Guid` | Unchanged | FK |
| `Name` | `string` | **Semantic change** | Full display name (e.g., "G", "Gm7", "Gadd9", "Gsus2"); no longer stores the root note letter alone |
| `Root` | `string` | **NEW** | Root note (e.g., "A", "F#", "Bb"); max 50 chars |
| `Quality` | `string` | **NEW** | One of 13 named qualities (see quality taxonomy below); max 50 chars |
| `Extension` | `string?` | **NEW** | Optional numeric/symbolic extension beyond the base quality (e.g., `"add9"` for Gadd9); null for most chords; max 50 chars |
| `Alternation` | `string?` | **NEW** | Optional chromatic alteration (e.g., `"#9"`, `"b5"`, `"#11"`); null for all current seeded chords; max 50 chars |
| `Suffix` | `string` | **DROPPED** | Removed — replaced by Quality + Extension + Alternation |
| `PositionsJson` | `string` | Unchanged | `NVARCHAR(MAX)` JSON array of positions |
| `Instrument` | `InstrumentEntity` | Unchanged | Navigation property |

#### Quality taxonomy (13 values)

| Quality string | Symbol | Meaning |
|---|---|---|
| `Major` | M | Major triad |
| `Major 7` | maj7 | Major triad + major 7th |
| `Sixth` | 6 | Major triad + major 6th |
| `Minor` | m | Minor triad |
| `Minor 7` | m7 | Minor triad + minor 7th |
| `Minor major 7` | m(maj7) | Minor triad + major 7th |
| `Seventh` | 7 | Dominant seventh (major triad + minor 7th) |
| `Diminished` | dim | Diminished triad |
| `Half-Diminished` | half-dim | Diminished triad + minor 7th (ø) |
| `Diminished 7th` | dim7 | Diminished triad + diminished 7th (°7) |
| `Suspended 4th` | sus4 | Suspended 4th triad |
| `Suspended 2nd` | sus2 | Suspended 2nd triad |
| `Augmented` | aug | Augmented triad |

---

### ChordConfiguration *(modify)*

**File**: `Persistence/Configurations/ChordConfiguration.cs`

```
Removed:
  builder.Property(c => c.Suffix).IsRequired().HasMaxLength(100);

Added:
  builder.Property(c => c.Root).IsRequired().HasMaxLength(50);
  builder.Property(c => c.Quality).IsRequired().HasMaxLength(50);
  builder.Property(c => c.Extension).HasMaxLength(50);      // nullable
  builder.Property(c => c.Alternation).HasMaxLength(50);    // nullable
  builder.HasIndex(c => new { c.InstrumentId, c.Root, c.Quality })
         .HasDatabaseName("IX_Chords_InstrumentId_Root_Quality");
```

---

## Domain Model Changes

### Chord *(modify)*

**File**: `DomainModels/Models/Chord.cs`

| Property | Type | Change | Notes |
|---|---|---|---|
| `Id` | `Guid` | Unchanged | |
| `InstrumentId` | `Guid` | Unchanged | |
| `InstrumentKey` | `InstrumentKey` | **NEW** | Denormalized from navigation; for response `instrumentKey` field |
| `Name` | `string` | **Semantic change** | Full display name (e.g., "Gm7", "Gsus2") |
| `Root` | `string` | **NEW** | Root note (e.g., "G") |
| `Quality` | `string` | **NEW** | Quality string (e.g., `"Minor 7"`, `"Seventh"`) |
| `Extension` | `string?` | **NEW** | Optional extension (e.g., `"add9"`); null for most |
| `Alternation` | `string?` | **NEW** | Optional alteration (e.g., `"#9"`); null for all seeded chords |
| `Suffix` | `string` | **REMOVED** | Dropped — use Quality + Extension + Alternation |
| `PositionsJson` | `string` | **REMOVED** | Replaced by `Positions` — deserialization in Repository layer |
| `Positions` | `List<ChordPosition>` | **NEW** | Deserialized from `PositionsJson` by AutoMapper |

---

### New Domain Models

#### ChordPosition *(new)*
**File**: `DomainModels/Models/ChordPosition.cs`

| Property | Type | Nullable | Notes |
|---|---|---|---|
| `Label` | `string` | No | Position label (e.g., "1") |
| `BaseFret` | `int` | No | Starting fret; minimum 1 (nut position); no maximum constraint |
| `Barre` | `ChordBarre?` | Yes | Optional barre descriptor |
| `Strings` | `List<ChordString>` | No | Exactly `instrument.StringCount` entries — one per string; partial arrays are invalid |

#### ChordBarre *(new)*
**File**: `DomainModels/Models/ChordBarre.cs`

| Property | Type | Notes |
|---|---|---|
| `Fret` | `int` | Fret number of the barre |
| `FromString` | `int` | Highest-pitched string (1 = high E) |
| `ToString` | `int` | Lowest-pitched string |

#### ChordString *(new)*
**File**: `DomainModels/Models/ChordString.cs`

| Property | Type | Nullable | Notes |
|---|---|---|---|
| `StringNumber` | `int` | No | String index (1 = highest pitched) |
| `State` | `ChordStringState` | No | `Open`, `Fretted`, or `Muted` |
| `Fret` | `int?` | Yes | Null when state is Open or Muted |
| `Finger` | `int?` | Yes | 1–4; null when Open or Muted |

> `ChordStringState` enum already exists in `DomainModels/Enums/ChordStringState.cs`.

---

## Response DTOs (ApiModels)

### InstrumentResponse *(new)*
**File**: `ApiModels/Instruments/InstrumentResponse.cs`

```csharp
record InstrumentResponse(
    Guid Id,
    string Key,        // InstrumentKey enum serialized as string
    string Name,       // DisplayName
    int StringCount
);
```

### ChordSummaryResponse *(new)*
**File**: `ApiModels/Chords/ChordSummaryResponse.cs`

```csharp
record ChordSummaryResponse(
    Guid Id,
    string InstrumentKey,
    string Name,              // "Gm7", "Gsus2", "G"
    string Root,              // "G"
    string Quality,           // "Minor 7", "Suspended 2nd", "Major"
    string? Extension,        // "add9" or null
    string? Alternation,      // "#9" or null
    ChordPositionResponse PreviewPosition
);
```

### ChordDetailResponse *(new)*
**File**: `ApiModels/Chords/ChordDetailResponse.cs`

```csharp
record ChordDetailResponse(
    Guid Id,
    string InstrumentKey,
    string Name,
    string Root,
    string Quality,
    string? Extension,
    string? Alternation,
    IReadOnlyList<ChordPositionResponse> Positions
);
```

### ChordPositionResponse *(new)*
**File**: `ApiModels/Chords/ChordPositionResponse.cs`

```csharp
record ChordPositionResponse(
    string Label,
    int BaseFret,
    ChordBarreResponse? Barre,
    IReadOnlyList<ChordStringResponse> Strings
);
```

### ChordBarreResponse *(new)*
**File**: `ApiModels/Chords/ChordBarreResponse.cs`

```csharp
record ChordBarreResponse(int Fret, int FromString, int ToString);
```

### ChordStringResponse *(new)*
**File**: `ApiModels/Chords/ChordStringResponse.cs`

```csharp
record ChordStringResponse(
    int String,       // JSON field named "string" — needs [JsonPropertyName("string")]
    string State,     // "open" | "fretted" | "muted"
    int? Fret,
    int? Finger
);
```

---

## Repository Mapping Changes

### EntityToDomainProfile *(modify)*
**File**: `Repository/Mapping/EntityToDomainProfile.cs`

Replace the existing `CreateMap<ChordEntity, Chord>().ReverseMap()` with a custom mapping:
- `ChordEntity.Instrument.Key → Chord.InstrumentKey` (via `.Include(c => c.Instrument)` in repository)
- `ChordEntity.PositionsJson → Chord.Positions` via `JsonSerializer.Deserialize<List<ChordPosition>>(src.PositionsJson)`
- Extension and Alternation map directly (nullable → nullable)
- Suffix is no longer present on either side

The reverse map (`Chord → ChordEntity`) is not needed for read-only chord data.

### DomainToResponseProfile *(modify)*
**File**: `Api/Mapping/DomainToResponseProfile.cs`

Add mappings:
- `Instrument → InstrumentResponse` — maps `DisplayName → Name`, `Key.ToString() → Key`
- `Chord → ChordSummaryResponse` — maps `InstrumentKey.ToString() → InstrumentKey`, `Positions[0] → PreviewPosition`, `Extension → Extension`, `Alternation → Alternation`
- `Chord → ChordDetailResponse` — maps all `Positions → Positions`, `InstrumentKey.ToString() → InstrumentKey`
- `ChordPosition → ChordPositionResponse`
- `ChordBarre → ChordBarreResponse`
- `ChordString → ChordStringResponse` — maps `StringNumber → String`, `State.ToString().ToLower() → State`

---

## EF Core Migration

**Name**: `RestructureChordSchema`

**Steps**:
1. `AddColumn: Chords.Root nvarchar(50) NOT NULL DEFAULT('')`
2. `AddColumn: Chords.Quality nvarchar(50) NOT NULL DEFAULT('')`
3. `AddColumn: Chords.Extension nvarchar(50) NULL`
4. `AddColumn: Chords.Alternation nvarchar(50) NULL`
5. Raw SQL migration to populate Root, Quality, Extension, and Name from existing Suffix data:

```sql
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
```

> All CASE expressions evaluate the original `Suffix` value before any assignment takes effect. Safe to run as a single `UPDATE`. On a fresh database the Chords table is empty — this is a safe no-op; the differential seeder then populates all rows from the new JSON format.

6. `AlterColumn` to remove defaults from Root and Quality
7. `DropColumn: Chords.Suffix`
8. `CreateIndex: IX_Chords_InstrumentId_Root_Quality`

> **Suffix column is dropped in this migration.** There is no backwards path — any code still referencing `ChordEntity.Suffix` will fail to compile after applying this migration.
