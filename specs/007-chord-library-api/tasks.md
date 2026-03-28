# Tasks: Chord Library API

**Input**: Design documents from `/specs/007-chord-library-api/`
**Prerequisites**: plan.md ✅, spec.md ✅, data-model.md ✅, contracts/endpoints.md ✅, research.md ✅

---

## Phase 1: Setup

No new project scaffolding required — implementing into an existing 9-project solution. Proceed directly to Phase 2.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Schema changes, new domain models, response DTOs, and repository mapping that every user story depends on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [x] T001 Update ChordEntity in `EntityModels/Entities/ChordEntity.cs`: add `Root` (string), `Quality` (string), `Extension` (string?, nullable), `Alternation` (string?, nullable); remove `Suffix` property
- [x] T002 [P] Create ChordPosition domain model in `DomainModels/Models/ChordPosition.cs`: properties `Label` (string), `BaseFret` (int), `Barre` (ChordBarre?), `Strings` (List\<ChordString\>) — see data-model.md §New Domain Models
- [x] T003 [P] Create ChordBarre domain model in `DomainModels/Models/ChordBarre.cs`: properties `Fret` (int), `FromString` (int), `ToString` (int)
- [x] T004 [P] Create ChordString domain model in `DomainModels/Models/ChordString.cs`: properties `StringNumber` (int), `State` (ChordStringState), `Fret` (int?), `Finger` (int?)
- [x] T005 Update Chord domain model in `DomainModels/Models/Chord.cs`: remove `Suffix` and `PositionsJson`; add `Root` (string), `Quality` (string), `Extension` (string?), `Alternation` (string?), `InstrumentKey` (InstrumentKey enum), `Positions` (List\<ChordPosition\>)
- [x] T006 Update ChordConfiguration in `Persistence/Configurations/ChordConfiguration.cs`: remove Suffix config; add `Root` (`IsRequired().HasMaxLength(50)`), `Quality` (`IsRequired().HasMaxLength(50)`), `Extension` (`HasMaxLength(50)` — nullable, no IsRequired), `Alternation` (`HasMaxLength(50)` — nullable, no IsRequired); add composite index `IX_Chords_InstrumentId_Root_Quality` on `(InstrumentId, Root, Quality)`
- [x] T007 Add EF Core migration `RestructureChordSchema` via `dotnet ef migrations add RestructureChordSchema --project Persistence/Persistence.csproj --startup-project Application/Application.csproj`, then edit the generated file: add columns with defaults → insert data population SQL (`UPDATE Chords SET Root=Name, Quality=CASE Suffix..., Extension=CASE Suffix..., Name=Name+CASE Suffix... WHERE Root=''`) → `AlterColumn` to remove defaults → `DropColumn Suffix` → `CreateIndex IX_Chords_InstrumentId_Root_Quality` — see data-model.md §EF Core Migration for the full SQL
- [x] T008 [P] Mark `guitar_chords.json` as EmbeddedResource in `Persistence/Persistence.csproj`: change `<Content Include="Data\guitar_chords.json" ...>` to `<EmbeddedResource Include="Data\guitar_chords.json" />`
- [x] T009 Update ChordSeeder in `Persistence/Seed/ChordSeeder.cs`: replace `virtual string ChordFilePath` property with `virtual Stream? GetChordStream()` method calling `typeof(ChordSeeder).Assembly.GetManifestResourceStream("Persistence.Data.guitar_chords.json")`; update private `ChordRecord` DTO to fields `Name`, `Root`, `Quality`, `Extension` (string?), `Alternation` (string?) — no Suffix; read stream via `new StreamReader(stream)` to strip UTF-8 BOM; replace skip-if-any with differential seeding using `HashSet<(Guid, string, string, string)>` of existing `(InstrumentId, Root, Quality, Extension ?? "")` tuples; map JSON fields directly: `Root = r.Root`, `Quality = r.Quality`, `Extension = r.Extension`, `Alternation = r.Alternation`; remove `Suffix` from all ChordEntity assignments
- [x] T010 [P] Update InstrumentSeeder in `Persistence/Seed/InstrumentSeeder.cs`: replace skip-if-any guard with per-record differential seeding; load existing instrument `Key` enum values into a `HashSet<InstrumentKey>`; for each source instrument, insert only those whose `Key` is absent from the HashSet
- [x] T011 [P] Create `ApiModels/Instruments/InstrumentResponse.cs`: `record InstrumentResponse(Guid Id, string Key, string Name, int StringCount)`
- [x] T012 [P] Create `ApiModels/Chords/ChordSummaryResponse.cs`: `record ChordSummaryResponse(Guid Id, string InstrumentKey, string Name, string Root, string Quality, string? Extension, string? Alternation, ChordPositionResponse PreviewPosition)` — no Suffix field
- [x] T013 [P] Create `ApiModels/Chords/ChordDetailResponse.cs`: `record ChordDetailResponse(Guid Id, string InstrumentKey, string Name, string Root, string Quality, string? Extension, string? Alternation, IReadOnlyList<ChordPositionResponse> Positions)`
- [x] T014 [P] Create `ApiModels/Chords/ChordPositionResponse.cs`: `record ChordPositionResponse(string Label, int BaseFret, ChordBarreResponse? Barre, IReadOnlyList<ChordStringResponse> Strings)`
- [x] T015 [P] Create `ApiModels/Chords/ChordBarreResponse.cs`: `record ChordBarreResponse(int Fret, int FromString, int ToString)`
- [x] T016 [P] Create `ApiModels/Chords/ChordStringResponse.cs`: `record ChordStringResponse([property: JsonPropertyName("string")] int String, string State, int? Fret, int? Finger)` — `[JsonPropertyName("string")]` required because `string` is a C# keyword
- [x] T017 Update EntityToDomainProfile in `Repository/Mapping/EntityToDomainProfile.cs`: replace `CreateMap<ChordEntity, Chord>().ReverseMap()` with an explicit one-way mapping; map `ChordEntity.Instrument.Key → Chord.InstrumentKey`; map `ChordEntity.PositionsJson → Chord.Positions` via `JsonSerializer.Deserialize<List<ChordPosition>>(src.PositionsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })`; no reverse map needed (read-only chord data)

**Checkpoint**: Schema is migrated, domain models are updated, DTOs exist, repository mapping is wired. User story implementation can begin.

---

## Phase 3: User Stories 1 & 2 — Chord Endpoints (Priority: P1) 🎯 MVP

**Goal**: Expose `GET /chords` (filtered chord list with preview position) and `GET /chords/{id}` (full chord detail). Both endpoints are public, read-only, and served from seeded data. US1 and US2 share all infrastructure — tasks are labelled [US1], [US2], or [US1][US2] accordingly.

**Independent Test for US1**: `GET /chords?instrument=Guitar6String` returns `200` with an array of `ChordSummaryResponse` objects each containing `previewPosition`. `GET /chords` (no instrument param) returns `400`. `GET /chords?instrument=Theremin` returns `400`.

**Independent Test for US2**: `GET /chords/{id}` with a seeded chord ID returns `200` with all positions. `GET /chords/{id}` with an unknown ID returns `404`.

- [x] T018 [P] [US1][US2] Add `GetByKeyAsync(InstrumentKey key, CancellationToken ct = default)` to `Domain/Interfaces/Repositories/IInstrumentRepository.cs` and implement in `Repository/Repositories/InstrumentRepository.cs`: `.Where(i => i.Key == key).AsNoTracking().FirstOrDefaultAsync(ct)` followed by AutoMapper map to `Instrument`; return null if not found
- [x] T019 [P] [US1][US2] Update ChordRepository in `Repository/Repositories/ChordRepository.cs`: in `SearchAsync`, fix root filter from `c.Name == root` to `c.Root.ToLower() == root.ToLower()` (skip if null), fix quality filter from `c.Suffix == quality` to `c.Quality.ToLower() == quality.ToLower()` (skip if null), add `.Include(c => c.Instrument)`, add `.OrderBy(c => c.Root).ThenBy(c => c.Quality)`; override `GetByIdAsync(Guid id, CancellationToken ct)` to query with `.Include(c => c.Instrument)` (base class omits navigation properties, causing `InstrumentKey` to be unset)
- [x] T020 [US1][US2] Create `Domain/Services/IChordService.cs` and `Domain/Services/ChordService.cs`: `SearchAsync(InstrumentKey instrumentKey, string? root, string? quality, CancellationToken ct)` — call `IInstrumentRepository.GetByKeyAsync` → throw `NotFoundException("INSTRUMENT_NOT_FOUND")` if null → call `IChordRepository.SearchAsync(instrument.Id, root, quality, ct)`; `GetByIdAsync(Guid id, CancellationToken ct)` — call `IChordRepository.GetByIdAsync(id, ct)` → throw `NotFoundException` if null
- [x] T021 [US1][US2] Update DomainToResponseProfile in `Api/Mapping/DomainToResponseProfile.cs`: add `Instrument → InstrumentResponse` mapping (`DisplayName → Name`, `Key.ToString() → Key`); add `Chord → ChordSummaryResponse` (`InstrumentKey.ToString() → InstrumentKey`, `Positions[0] → PreviewPosition`, `Extension → Extension`, `Alternation → Alternation`); add `Chord → ChordDetailResponse` (`InstrumentKey.ToString() → InstrumentKey`, `Positions → Positions`); add `ChordPosition → ChordPositionResponse`; add `ChordBarre → ChordBarreResponse`; add `ChordString → ChordStringResponse` (`StringNumber → String`, `State.ToString().ToLower() → State`) — no Suffix mapping anywhere
- [x] T022 [US1][US2] Create `Api/Controllers/ChordsController.cs` with route `[Route("api/chords")]`: `GET /chords` — no `[Authorize]`, `[FromQuery] InstrumentKey instrument` (model binding returns 400 for invalid/missing values), optional `string? root`, `string? quality`, `[ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]`, maps result to `IReadOnlyList<ChordSummaryResponse>`; `GET /chords/{id}` — no `[Authorize]`, `[ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]`, maps result to `ChordDetailResponse`
- [x] T023 Register `IChordService`/`ChordService` as `AddScoped` in `Application/Extensions/ServiceCollectionExtensions.cs`; add `services.AddResponseCaching()` in the same pass
- [x] T024 [P] Add `app.UseResponseCaching()` to `Application/Program.cs` pipeline immediately before `app.UseAuthentication()`
- [x] T025 [P after T020] [US1][US2] Create `Tests/Unit/Services/ChordServiceTests.cs`: happy path — `SearchAsync` returns chord list from repository; happy path — `GetByIdAsync` returns chord with positions; exception path — `SearchAsync` with unknown instrument key throws `NotFoundException`; exception path — `GetByIdAsync` with unknown id throws `NotFoundException`
- [x] T026 [US1][US2] Create `Tests/Integration/Controllers/ChordsControllerTests.cs`: `GET /chords?instrument=Guitar6String` → 200 with summaries; `GET /chords?instrument=Guitar6String&root=A&quality=Major` → 200 filtered; `GET /chords` → 400; `GET /chords?instrument=Invalid` → 400; `GET /chords/{id}` with seeded chord id → 200 with all positions; `GET /chords/{id}` with random Guid → 404

**Checkpoint**: Chord list and chord detail endpoints are independently functional and tested. MVP is deployable.

---

## Phase 4: User Story 3 — Instruments Endpoint (Priority: P2)

**Goal**: Expose `GET /instruments` returning all seeded instrument records. Public endpoint, no authentication required.

**Independent Test**: `GET /instruments` without an `Authorization` header returns `200` with an array of all instrument records, each containing `id`, `key`, `name`, and `stringCount`.

- [x] T027 [US3] Create `Domain/Services/IInstrumentService.cs` and `Domain/Services/InstrumentService.cs`: `GetAllAsync(CancellationToken ct = default)` — inject `IInstrumentRepository`, delegate to `IInstrumentRepository.GetAllAsync(ct)`, return `IReadOnlyList<Instrument>`
- [x] T028 [US3] Register `IInstrumentService`/`InstrumentService` as `AddScoped` in `Application/Extensions/ServiceCollectionExtensions.cs` (same file as T023 — add the second registration)
- [x] T029 [US3] Create `Api/Controllers/InstrumentsController.cs` with route `[Route("api/instruments")]`: `GET /instruments` — no `[Authorize]`; call `IInstrumentService.GetAllAsync(ct)`; map to `IReadOnlyList<InstrumentResponse>`; return `Ok(result)`
- [x] T030 [P] [US3] Create `Tests/Unit/Services/InstrumentServiceTests.cs`: happy path — `GetAllAsync` returns all instruments returned by the repository mock
- [x] T031 [US3] Create `Tests/Integration/Controllers/InstrumentsControllerTests.cs`: `GET /instruments` → 200 with all seeded instruments; `GET /instruments` without `Authorization` header → 200 (public endpoint, no auth required)

**Checkpoint**: All three public chord library endpoints are functional. Feature is complete.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Restore seeder test coverage after the embedded-resource and schema changes in Phase 2.

- [ ] T032 Update `Tests/Unit/Persistence/ChordSeederHappyPathTests.cs`: change `TestableChordSeeder` override from `ChordFilePath` (string) to `GetChordStream()` (Stream?); update all inline test JSON payloads to new format (`name`, `root`, `quality`, `extension`, `alternation` fields — no `suffix`); verify existing `chord.Name == "C"` assertion still holds for Major chords (C major name = root letter); add test `SeedAsync_PartiallySeeded_InsertsOnlyNewChords` — seed 2 chords, call again with 3, assert total count = 3
- [ ] T033 [P] Update `Tests/Unit/Persistence/ChordSeederFailTests.cs`: update `TestableChordSeeder` override to `GetChordStream()` returning a `MemoryStream`; remove tests for "file not found" and "failed to read file path" (no longer applicable); keep tests for: invalid JSON, null/empty stream, empty positions array, missing required fields (`root`, `quality`)
- [ ] T034 [P] Update `Tests/Unit/Mapping/DomainToResponseProfileTests.cs`: add mapping assertion tests for the 6 new profiles added in T021 — `Instrument → InstrumentResponse` (Key as string, DisplayName → Name), `Chord → ChordSummaryResponse` (Positions[0] → PreviewPosition, InstrumentKey as string, nullable Extension/Alternation pass-through), `Chord → ChordDetailResponse`, `ChordPosition → ChordPositionResponse`, `ChordBarre → ChordBarreResponse`, `ChordString → ChordStringResponse` (StringNumber → String, State lowercase string)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 2)**: No dependencies — can start immediately
- **US1+US2 (Phase 3)**: BLOCKED on Phase 2 completion (requires T001-T017)
- **US3 (Phase 4)**: No hard block on Phase 3 completion — T027–T031 can start as soon as Phase 2 is done. T028 (DI registration for IInstrumentService) must follow T027 (type creation). T029 (InstrumentsController) depends on T027 and T028.
- **Polish (Phase 5)**: BLOCKED on T009 (ChordSeeder changes in Phase 2) and T021 (DomainToResponseProfile mappings in Phase 3)

### Within Phase 2

Sequence: T001 → T006 → T007 (schema chain). T005 depends on T002-T004. All others ([P]-marked) are independent and can run simultaneously.

### Within Phase 3

1. T018 and T019 — no dependencies, run simultaneously
2. T020 — depends on T018, T019
3. T021, T023, T025 — depend on T020, run simultaneously once T020 is done
4. T022 — depends on T020 and T021
5. T024 — depends on T023
6. T026 — depends on T022

---

## Parallel Example: Phase 2

```
Simultaneously (all different files, no cross-dependencies):
  T002 — DomainModels/Models/ChordPosition.cs
  T003 — DomainModels/Models/ChordBarre.cs
  T004 — DomainModels/Models/ChordString.cs
  T008 — Persistence/Persistence.csproj (EmbeddedResource)
  T010 — Persistence/Seed/InstrumentSeeder.cs
  T011 — ApiModels/Instruments/InstrumentResponse.cs
  T012 — ApiModels/Chords/ChordSummaryResponse.cs
  T013 — ApiModels/Chords/ChordDetailResponse.cs
  T014 — ApiModels/Chords/ChordPositionResponse.cs
  T015 — ApiModels/Chords/ChordBarreResponse.cs
  T016 — ApiModels/Chords/ChordStringResponse.cs
```

## Parallel Example: Phase 3

```
Start simultaneously:
  T018 — IInstrumentRepository + InstrumentRepository (GetByKeyAsync)
  T019 — ChordRepository (SearchAsync + GetByIdAsync override)

After T018 + T019 complete:
  T020 — IChordService + ChordService

After T020 completes, start simultaneously:
  T021 — DomainToResponseProfile (chord + instrument mappings)
  T023 — ServiceCollectionExtensions (DI + AddResponseCaching)
  T025 — ChordServiceTests

After T021 + T023 complete:
  T022 — ChordsController  →  then T026 (ChordsControllerTests)
  T024 — Program.cs (UseResponseCaching)
```

---

## Implementation Strategy

### MVP First (US1 + US2 Only)

1. Complete Phase 2: Foundational (17 tasks — schema, models, DTOs, mapping)
2. Complete Phase 3: US1 + US2 (9 tasks — chord list + chord detail)
3. **STOP and VALIDATE**: `GET /chords?instrument=Guitar6String` returns populated chord summaries with `previewPosition`; `GET /chords/{id}` returns full positions with `barre` and `strings`
4. Deploy/demo if ready

### Incremental Delivery

1. Phase 2 (Foundational) → database migrated, chord data seeded from embedded JSON
2. Phase 3 (US1 + US2) → chord browsing and detail live — MVP shipped
3. Phase 4 (US3) → instrument picker endpoint added
4. Phase 5 (Polish) → seeder test coverage fully restored; DomainToResponseProfile mapping tests added

---

## Notes

- `[P]` tasks have no blocking dependencies within their phase and touch different files — safe to run in parallel
- `[US1]`/`[US2]`/`[US3]` labels trace each task to its user story for independent verification
- T023 registers only `IChordService` (Phase 3); T028 registers `IInstrumentService` (Phase 4) after T027 creates the type — compile order is correct
- `InstrumentSeeder` must execute before `ChordSeeder` in `DbInitializer` — already satisfied in source (`DbInitializer.cs` calls seeders in the correct order); no task required
- Seeder natural keys: Instrument = `Key` enum value; Chord = `(InstrumentId, Root, Quality, Extension ?? "")`
- Total tasks: 34 (T001–T034)
