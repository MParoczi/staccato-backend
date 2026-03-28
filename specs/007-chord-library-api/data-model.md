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
| `Name` | `string` | **Semantic change** | Now stores display name (e.g., "A major"), not root note letter |
| `Root` | `string` | **NEW** | Root note (e.g., "A", "F#", "Bb"); max 50 chars |
| `Quality` | `string` | **NEW** | Quality string (e.g., "major", "min7"); max 100 chars |
| `Suffix` | `string` | Unchanged | Same as Quality from JSON; max 100 chars |
| `PositionsJson` | `string` | Unchanged | `NVARCHAR(MAX)` JSON array of positions |
| `Instrument` | `InstrumentEntity` | Unchanged | Navigation property |

---

### ChordConfiguration *(modify)*

**File**: `Persistence/Configurations/ChordConfiguration.cs`

```
Added:
  builder.Property(c => c.Root).IsRequired().HasMaxLength(50);
  builder.Property(c => c.Quality).IsRequired().HasMaxLength(100);
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
| `Name` | `string` | **Semantic change** | Display name (e.g., "A major") |
| `Root` | `string` | **NEW** | Root note (e.g., "A") |
| `Quality` | `string` | **NEW** | Quality (e.g., "major") |
| `Suffix` | `string` | Unchanged | |
| `PositionsJson` | `string` | **REMOVED** | Replaced by `Positions` — deserialization in Repository layer |
| `Positions` | `List<ChordPosition>` | **NEW** | Deserialized from `PositionsJson` by AutoMapper |

---

### New Domain Models

#### ChordPosition *(new)*
**File**: `DomainModels/Models/ChordPosition.cs`

| Property | Type | Nullable | Notes |
|---|---|---|---|
| `Label` | `string` | No | Position label (e.g., "1") |
| `BaseFret` | `int` | No | Starting fret (1 = nut) |
| `Barre` | `ChordBarre?` | Yes | Optional barre descriptor |
| `Strings` | `List<ChordString>` | No | One entry per string |

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
    string Name,              // "A major"
    string Root,              // "A"
    string Quality,           // "major"
    string Suffix,            // "major"
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
    string Suffix,
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
    int String,       // JSON field named "string"
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
- `.ForMember(d => d.PositionsJson, ...)` — N/A since `Chord` no longer has `PositionsJson`

The reverse map (`Chord → ChordEntity`) is not needed for read-only chord data.

### DomainToResponseProfile *(modify)*
**File**: `Api/Mapping/DomainToResponseProfile.cs`

Add mappings:
- `Instrument → InstrumentResponse` — maps `DisplayName → Name`, `Key.ToString() → Key`
- `Chord → ChordSummaryResponse` — maps `Positions[0] → PreviewPosition`, `Key.ToString() → InstrumentKey`
- `Chord → ChordDetailResponse` — maps all `Positions → Positions`, `Key.ToString() → InstrumentKey`
- `ChordPosition → ChordPositionResponse`
- `ChordBarre → ChordBarreResponse`
- `ChordString → ChordStringResponse` — maps `StringNumber → String`, `State.ToString().ToLower() → State`

---

## EF Core Migration

**Name**: `AddChordRootAndQuality`

**Steps**:
1. `AddColumn: Chords.Root nvarchar(200) NOT NULL DEFAULT('')`
2. `AddColumn: Chords.Quality nvarchar(200) NOT NULL DEFAULT('')`
3. `Sql("UPDATE Chords SET Root = Name, Quality = Suffix, Name = Name + ' ' + Suffix WHERE Root = ''")`
4. `AlterColumn` to remove defaults (columns are always populated by seeder)
5. `CreateIndex: IX_Chords_InstrumentId_Root_Quality`

> Step 3 is a raw SQL migration required because the Root/Quality columns cannot be derived by EF automatically from existing data.
