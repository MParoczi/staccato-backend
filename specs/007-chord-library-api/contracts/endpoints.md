# API Contracts: Chord Library API

**Feature**: 007-chord-library-api
**Base URL**: `/api`
**Authentication**: None required for any endpoint in this feature

---

## GET /instruments

Returns all seeded instrument records.

**Query parameters**: none
**Authentication**: none
**Response caching**: none (infrequently called; not worth caching overhead)

### Response 200

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "key": "Guitar6String",
    "name": "6-String Guitar",
    "stringCount": 6
  },
  {
    "id": "4cb96e75-6828-5673-c4gd-3d074g77bgb7",
    "key": "Guitar7String",
    "name": "7-String Guitar",
    "stringCount": 7
  }
]
```

### Field details
| Field | Type | Source |
|---|---|---|
| `id` | UUID | `InstrumentEntity.Id` |
| `key` | string (enum) | `InstrumentEntity.Key.ToString()` |
| `name` | string | `InstrumentEntity.DisplayName` |
| `stringCount` | int | `InstrumentEntity.StringCount` |

---

## GET /chords

Returns chord summaries filtered by instrument key, with optional root and quality filters.

**Authentication**: none
**Response caching**: `Cache-Control: public, max-age=300` (5 minutes)

### Query parameters

| Parameter | Required | Type | Example | Notes |
|---|---|---|---|---|
| `instrument` | **Yes** | `InstrumentKey` enum string | `Guitar6String` | Missing → 400; invalid value → 400 |
| `root` | No | string | `F`, `C#`, `Bb` | Case-insensitive; omit to return all roots |
| `quality` | No | string | `Minor 7`, `Seventh` | Case-insensitive; one of 13 named qualities (see spec §Key Entities); omit to return all qualities |

### Response 200

```json
[
  {
    "id": "uuid-of-F-major",
    "instrumentKey": "Guitar6String",
    "name": "F",
    "root": "F",
    "quality": "Major",
    "extension": null,
    "alternation": null,
    "previewPosition": {
      "label": "1",
      "baseFret": 1,
      "barre": { "fret": 1, "fromString": 1, "toString": 6 },
      "strings": [
        { "string": 6, "state": "fretted", "fret": 1, "finger": 1 },
        { "string": 5, "state": "fretted", "fret": 3, "finger": 3 },
        { "string": 4, "state": "fretted", "fret": 3, "finger": 4 },
        { "string": 3, "state": "fretted", "fret": 2, "finger": 2 },
        { "string": 2, "state": "fretted", "fret": 1, "finger": 1 },
        { "string": 1, "state": "fretted", "fret": 1, "finger": 1 }
      ]
    }
  }
]
```

**Empty result**: `[]` when no chords match filters (not an error).

> **Field semantics**: `quality` is one of 13 named chord quality values (`"Major"`, `"Minor 7"`, `"Seventh"`, `"Half-Diminished"`, etc.) that fully describes the chord's harmonic type. `extension` is an optional symbolic extension beyond the base quality (e.g. `"add9"` for an added 9th; `null` for most chords). `alternation` is an optional chromatic alteration (e.g. `"#9"`, `"b5"`; `null` for all currently seeded chords). `name` is the full conventional display name (e.g. `"F"`, `"Am7"`, `"Gadd9"`, `"Gsus2"`). There is no `suffix` field.

### Response 400 — missing `instrument`

```json
{
  "errors": {
    "instrument": ["The instrument field is required."]
  }
}
```

### Response 400 — invalid `instrument` value

```json
{
  "errors": {
    "instrument": ["'Theremin' is not a valid InstrumentKey."]
  }
}
```

### Ordering
Results are ordered by `Root` ascending, then `Quality` ascending.

### `previewPosition`
Always the first element (index 0) of the chord's stored positions array.

---

## GET /chords/{id}

Returns the full chord detail including all positions.

**Authentication**: none
**Response caching**: `Cache-Control: public, max-age=300` (5 minutes)

### Path parameters
| Parameter | Type | Notes |
|---|---|---|
| `id` | UUID | Chord identifier |

### Response 200

```json
{
  "id": "uuid-of-F-major",
  "instrumentKey": "Guitar6String",
  "name": "F",
  "root": "F",
  "quality": "Major",
  "extension": null,
  "alternation": null,
  "positions": [
    {
      "label": "1",
      "baseFret": 1,
      "barre": { "fret": 1, "fromString": 1, "toString": 6 },
      "strings": [
        { "string": 6, "state": "fretted", "fret": 1, "finger": 1 },
        { "string": 5, "state": "fretted", "fret": 3, "finger": 3 },
        { "string": 4, "state": "fretted", "fret": 3, "finger": 4 },
        { "string": 3, "state": "fretted", "fret": 2, "finger": 2 },
        { "string": 2, "state": "fretted", "fret": 1, "finger": 1 },
        { "string": 1, "state": "fretted", "fret": 1, "finger": 1 }
      ]
    },
    {
      "label": "2",
      "baseFret": 8,
      "barre": null,
      "strings": [
        { "string": 6, "state": "muted", "fret": null, "finger": null },
        { "string": 5, "state": "fretted", "fret": 8, "finger": 1 },
        { "string": 4, "state": "fretted", "fret": 10, "finger": 4 },
        { "string": 3, "state": "fretted", "fret": 10, "finger": 3 },
        { "string": 2, "state": "fretted", "fret": 10, "finger": 2 },
        { "string": 1, "state": "muted", "fret": null, "finger": null }
      ]
    }
  ]
}
```

### Response 404

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Not Found",
  "status": 404,
  "detail": "Chord not found."
}
```

> 404 is returned via `BusinessExceptionMiddleware` handling `NotFoundException`.

---

## ChordString state values

| JSON value | Meaning |
|---|---|
| `"open"` | String is played open (○ above nut) |
| `"fretted"` | String is fretted at the given fret position |
| `"muted"` | String is muted (✕ above nut) |

When `state` is `"open"` or `"muted"`, `fret` and `finger` are `null`.

---

## Error codes for this feature

| Code | HTTP | Trigger |
|---|---|---|
| `INSTRUMENT_NOT_FOUND` | 404 | Instrument key is valid enum value but not in database (edge case) |
| *(validation)* | 400 | `instrument` param missing or not a valid `InstrumentKey` |
| *(not found)* | 404 | `GET /chords/{id}` where id does not match any chord |
