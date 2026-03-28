# Tasks: User Profile Management

**Input**: Design documents from `/specs/006-user-profile-management/`
**Branch**: `006-user-profile-management`
**Spec**: spec.md | **Plan**: plan.md | **Contracts**: contracts/api.md | **Data model**: data-model.md

**Organization**: Tasks grouped by user story. Each phase is independently deliverable and testable.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no shared dependency)
- **[Story]**: User story label (US1–US5)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: No new project scaffolding needed — this is an existing 9-project solution. Phase 1 verifies the foundation and creates the only new directory required.

- [x] T001 Confirm `Azure.Storage.Blobs` is present in `Application/Application.csproj` (verify existing reference before writing AzureBlobService)
- [x] T002 Create directory `Api/Mapping/` (new — no profile files exist there yet)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Cross-cutting infrastructure that all user stories depend on. No user story work can begin until this phase is complete.

**⚠️ CRITICAL**: Complete all tasks in this phase before moving to Phase 3.

- [x] T003 Create `Domain/Exceptions/BadRequestException.cs` — `BadRequestException : BusinessException` with `StatusCode = 400`; constructor accepts `(string code, string message, object? details = null)`
- [x] T004 [P] Add `PageSize? DefaultPageSize` and `Guid? DefaultInstrumentId` and `InstrumentEntity? DefaultInstrument` to `EntityModels/Entities/UserEntity.cs`
- [x] T005 [P] Add `PageSize? DefaultPageSize` and `Guid? DefaultInstrumentId` to `DomainModels/Models/User.cs`
- [x] T006 Update `Persistence/Configurations/UserConfiguration.cs` — add `.HasConversion<string>().HasColumnType("nvarchar(50)")` for `DefaultPageSize`; add `HasOne<InstrumentEntity>().WithMany().HasForeignKey(u => u.DefaultInstrumentId).IsRequired(false).OnDelete(DeleteBehavior.Restrict)` (depends on T004)
- [x] T007 Run `dotnet ef migrations add AddUserPreferences --project Persistence/Persistence.csproj --startup-project Application/Application.csproj` and verify generated migration adds `DefaultPageSize nvarchar(50) NULL` and `DefaultInstrumentId uniqueidentifier NULL` with `NO ACTION` FK to `Instruments` (depends on T006)
- [x] T008 [P] Create `Domain/Services/IAzureBlobService.cs` — interface with three methods: `Task<string> UploadAsync(Stream content, string contentType, string blobPath, CancellationToken ct = default)`, `Task DeleteAsync(string blobPath, CancellationToken ct = default)`, `Task<Stream?> GetStreamAsync(string blobPath, CancellationToken ct = default)`
- [x] T009 Create `Application/Services/AzureBlobService.cs` — inject `BlobServiceClient` and `IOptions<AzureBlobOptions>`; `UploadAsync` calls `GetBlobContainerClient(options.ContainerName).GetBlobClient(blobPath).UploadAsync(content, overwrite:true)` and returns `blobClient.Uri.ToString()`; `DeleteAsync` calls `DeleteBlobIfExistsAsync`; `GetStreamAsync` calls `DownloadStreamingAsync`, returns stream or null on `RequestFailedException 404` (depends on T008)
- [x] T010 [P] Create `Domain/Services/IUserService.cs` — declare all 10 method signatures: `GetProfileAsync`, `UpdateProfileAsync`, `ScheduleDeletionAsync`, `CancelDeletionAsync`, `UploadAvatarAsync`, `DeleteAvatarAsync`, `GetPresetsAsync`, `CreatePresetAsync`, `UpdatePresetAsync`, `DeletePresetAsync` — all accept `CancellationToken ct = default`; return types use `User` and `UserSavedPreset` domain models
- [x] T011 Create `Domain/Services/UserService.cs` — constructor injecting `IUserRepository`, `IUserSavedPresetRepository`, `IInstrumentRepository`, `IAzureBlobService`, `IUnitOfWork`; all 10 methods implemented as stubs throwing `NotImplementedException` (depends on T010; will be filled per story phase)
- [x] T012 Register services in `Application/Extensions/ServiceCollectionExtensions.cs` inside `AddDomainServices()`: add `services.AddScoped<IUserService, UserService>()` and `services.AddSingleton<IAzureBlobService, AzureBlobService>()` (depends on T009, T011)
- [x] T013 Create `Api/Controllers/UsersController.cs` — `[ApiController]`, `[Route("users")]`, `[Authorize]` on class; inject `IUserService` and `IMapper`; private helper `GetUserId()` returns `Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)`; no action methods yet (depends on T010)

**Checkpoint**: `dotnet build Staccato.sln` passes with zero errors. Migration exists. DI wired.

---

## Phase 3: User Story 1 — View and Update Profile (Priority: P1) 🎯 MVP

**Goal**: Authenticated users can retrieve their full profile and update all editable preferences.

**Independent Test**: Authenticate a user → `GET /users/me` returns full profile → `PUT /users/me` with updated values → second `GET /users/me` reflects changes.

- [x] T014 [P] Create `ApiModels/Users/UserResponse.cs` — record (or class) with: `Guid Id`, `string Email`, `string FirstName`, `string LastName`, `string Language`, `string? DefaultPageSize`, `Guid? DefaultInstrumentId`, `string? AvatarUrl`, `DateTime? ScheduledDeletionAt` — enum types stored as strings to keep ApiModels dependency-free
- [x] T015 [P] Create `ApiModels/Users/UpdateProfileRequest.cs` — record with: `string FirstName`, `string LastName`, `string Language`, `string? DefaultPageSize`, `Guid? DefaultInstrumentId`
- [x] T016 Create `ApiModels/Users/UpdateProfileRequestValidator.cs` — `RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100)`; `RuleFor(x => x.LastName).MaximumLength(100)`; `RuleFor(x => x.Language).NotEmpty().Must(v => v == "en" || v == "hu").WithMessage("Language must be 'en' or 'hu'.")`; `RuleFor(x => x.DefaultPageSize).Must(v => v == null || new[] { "A4", "A5", "A6", "B5", "B6" }.Contains(v)).WithMessage("DefaultPageSize must be one of: A4, A5, A6, B5, B6.")` (depends on T015; no DomainModels reference — validates strings inline)
- [x] T017 Create `Api/Mapping/DomainToResponseProfile.cs` — `DomainToResponseProfile : Profile`; add `CreateMap<User, UserResponse>()` with `.ForMember(d => d.Language, o => o.MapFrom(s => s.Language.ToString()))` and `.ForMember(d => d.DefaultPageSize, o => o.MapFrom(s => s.DefaultPageSize.HasValue ? s.DefaultPageSize.ToString() : null))` (depends on T014; explicit string conversion required — AutoMapper does not auto-convert enum to string)
- [x] T018 [US1] Implement `UserService.GetProfileAsync(Guid userId, CancellationToken ct)` in `Domain/Services/UserService.cs` — call `_userRepo.GetByIdAsync(userId, ct)`; throw `NotFoundException` if null; return user domain model
- [x] T019 [US1] Implement `UserService.UpdateProfileAsync(Guid userId, string firstName, string lastName, Language language, PageSize? defaultPageSize, Guid? defaultInstrumentId, CancellationToken ct)` in `Domain/Services/UserService.cs` — load user (NotFoundException if missing); if `defaultInstrumentId != null` verify instrument exists via `_instrumentRepo.GetByIdAsync` (throw `NotFoundException("INSTRUMENT_NOT_FOUND", ...)` if absent); update fields; call `_uow.CommitAsync(ct)`; return updated user
- [x] T020 [US1] Add `GetProfile` action to `Api/Controllers/UsersController.cs` — `[HttpGet("me")]` returning `Ok(_mapper.Map<UserResponse>(await _userService.GetProfileAsync(GetUserId(), ct)))`
- [x] T021 [US1] Add `UpdateProfile` action to `Api/Controllers/UsersController.cs` — `[HttpPut("me")]` accepting `UpdateProfileRequest request`; parse `Enum.Parse<Language>(request.Language)` and `request.DefaultPageSize != null ? Enum.Parse<PageSize>(request.DefaultPageSize) : (PageSize?)null` before calling `UpdateProfileAsync`; return `Ok(_mapper.Map<UserResponse>(updatedUser))` (enum parsing is safe here because validator in T016 already rejected invalid values)

**Checkpoint**: `GET /users/me` and `PUT /users/me` work end-to-end. `dotnet build` passes.

---

## Phase 4: User Story 2 — Schedule and Cancel Account Deletion (Priority: P1)

**Goal**: Users can schedule their account for soft-deletion with a 30-day grace period and cancel before it executes.

**Independent Test**: `DELETE /users/me` → 204 → `GET /users/me` returns `scheduledDeletionAt` set ~30 days out → `POST /users/me/cancel-deletion` → 204 → `GET /users/me` returns `scheduledDeletionAt: null`.

- [x] T022 [US2] Implement `UserService.ScheduleDeletionAsync(Guid userId, CancellationToken ct)` in `Domain/Services/UserService.cs` — load user; if `ScheduledDeletionAt != null` throw `ConflictException("ACCOUNT_DELETION_ALREADY_SCHEDULED", ...)`; set `ScheduledDeletionAt = DateTime.UtcNow.AddDays(30)`; call `_uow.CommitAsync(ct)`
- [x] T023 [US2] Implement `UserService.CancelDeletionAsync(Guid userId, CancellationToken ct)` in `Domain/Services/UserService.cs` — load user; if `ScheduledDeletionAt == null` throw `BadRequestException("ACCOUNT_DELETION_NOT_SCHEDULED", ...)`; set `ScheduledDeletionAt = null`; call `_uow.CommitAsync(ct)`
- [x] T024 [US2] Add `ScheduleDeletion` action to `Api/Controllers/UsersController.cs` — `[HttpDelete("me")]` calling `await _userService.ScheduleDeletionAsync(GetUserId(), ct)`; return `NoContent()`
- [x] T025 [US2] Add `CancelDeletion` action to `Api/Controllers/UsersController.cs` — `[HttpPost("me/cancel-deletion")]` calling `await _userService.CancelDeletionAsync(GetUserId(), ct)`; return `NoContent()`

**Checkpoint**: Deletion scheduling and cancellation work. 409 on double-schedule, 400 on cancel-when-not-scheduled.

---

## Phase 5: User Story 3 — Upload and Remove Avatar (Priority: P2)

**Goal**: Users can upload a profile avatar (JPG/PNG/WebP, ≤ 2,097,152 bytes) and remove it.

**Independent Test**: `PUT /users/me/avatar` with valid image → 200 with non-null `avatarUrl` → `DELETE /users/me/avatar` → 204 → `GET /users/me` returns `avatarUrl: null`. Invalid file → 400.

- [x] T026 [US3] Implement `UserService.UploadAvatarAsync(Guid userId, Stream stream, string contentType, CancellationToken ct)` in `Domain/Services/UserService.cs` — load user; if `user.AvatarUrl != null` call `_blobService.DeleteAsync(ExtractBlobPath(user.AvatarUrl), ct)` (log warning on failure, don't rethrow); call `_blobService.UploadAsync(stream, contentType, $"avatars/{userId}", ct)`; set `user.AvatarUrl = returned URL`; call `_uow.CommitAsync(ct)`; return updated user. Add private helper `ExtractBlobPath(string url)` that returns the blob path segment from the full URL.
- [x] T027 [US3] Implement `UserService.DeleteAvatarAsync(Guid userId, CancellationToken ct)` in `Domain/Services/UserService.cs` — load user; if `user.AvatarUrl == null` return (no-op); call `_blobService.DeleteAsync(ExtractBlobPath(user.AvatarUrl), ct)`; set `user.AvatarUrl = null`; call `_uow.CommitAsync(ct)`
- [x] T028 [US3] Add `UploadAvatar` action to `Api/Controllers/UsersController.cs` — `[HttpPut("me/avatar")]` accepting `IFormFile file`; validate `file != null` (400 if missing); validate `file.Length <= 2_097_152` (400 if exceeded); validate `file.ContentType` is one of `image/jpeg`, `image/png`, `image/webp` (400 if invalid); call `_userService.UploadAvatarAsync(GetUserId(), file.OpenReadStream(), file.ContentType, ct)`; return `Ok(_mapper.Map<UserResponse>(updatedUser))`
- [x] T029 [US3] Add `DeleteAvatar` action to `Api/Controllers/UsersController.cs` — `[HttpDelete("me/avatar")]` calling `await _userService.DeleteAvatarAsync(GetUserId(), ct)`; return `NoContent()`

**Checkpoint**: Avatar upload returns 200 with updated `avatarUrl`. Invalid inputs return 400. `DELETE /users/me/avatar` is idempotent (204 even when no avatar set).

---

## Phase 6: User Story 4 — Manage Style Presets (Priority: P2)

**Goal**: Users can create, list, update, and delete named style presets; names are unique per user.

**Independent Test**: `POST /users/me/presets` with valid 12-style body → 201 → `GET /users/me/presets` returns it → `PUT /users/me/presets/{id}` renames → `DELETE /users/me/presets/{id}` → 204 → `GET /users/me/presets` returns empty array.

- [ ] T030 [P] Create `ApiModels/Users/StyleEntryDto.cs` — record with `string ModuleType` and `string StylesJson`
- [ ] T031 [P] Create `ApiModels/Users/PresetResponse.cs` — record with `Guid Id`, `string Name`, `List<StyleEntryDto> Styles`
- [ ] T032 [P] Create `ApiModels/Users/SavePresetRequest.cs` — record with `string Name` and `IList<StyleEntryDto> Styles`
- [ ] T033 Create `ApiModels/Users/SavePresetRequestValidator.cs` — `RuleFor(x => x.Name).NotEmpty().MaximumLength(100)`; `RuleFor(x => x.Styles).NotNull().Must(s => s != null && s.Count == 12).WithMessage("Exactly 12 style entries required.")`; add `private static readonly HashSet<string> ValidModuleTypes = new(StringComparer.OrdinalIgnoreCase) { "Title", "Breadcrumb", "Text", "BulletList", "NumberedList", "CheckboxList", "Table", "MusicalNotes", "ChordProgression", "ChordTablatureGroup", "Date", "SectionHeading" };` and three custom rules on `Styles`: (1) `.Must(entries => entries.All(e => ValidModuleTypes.Contains(e.ModuleType))).WithMessage("Each moduleType must be a valid ModuleType value.")`, (2) `.Must(entries => entries.Select(e => e.ModuleType.ToLowerInvariant()).Distinct().Count() == entries.Count).WithMessage("Duplicate moduleType values are not allowed.")`, (3) `.Must(entries => entries.All(e => !string.IsNullOrEmpty(e.StylesJson))).WithMessage("Each stylesJson must be a non-empty string.")` (depends on T030, T032; no DomainModels reference — validates strings against hardcoded set)
- [ ] T034 [P] Create `ApiModels/Users/UpdatePresetRequest.cs` — record with `string? Name` and `IList<StyleEntryDto>? Styles`
- [ ] T035 Create `ApiModels/Users/UpdatePresetRequestValidator.cs` — custom top-level rule: `Must(r => r.Name != null || r.Styles != null).WithMessage("At least one of name or styles must be provided.")`; `When(x => x.Name != null, () => RuleFor(x => x.Name).NotEmpty().MaximumLength(100))`; `When(x => x.Styles != null, ...)` block applies same four rules as T033: count == 12, all entries in `ValidModuleTypes` set, no duplicate moduleType strings, all `stylesJson` non-empty — reuse the same static `ValidModuleTypes` HashSet (depends on T030, T034; no DomainModels reference)
- [ ] T036 Update `Api/Mapping/DomainToResponseProfile.cs` — add `CreateMap<UserSavedPreset, PresetResponse>().ForMember(d => d.Styles, o => o.MapFrom(s => JsonSerializer.Deserialize<List<StyleEntryDto>>(s.StylesJson, (JsonSerializerOptions?)null)))` (depends on T017, T031)
- [ ] T037 Add `ExistsByNameAsync(Guid userId, string name, Guid? excludePresetId = null, CancellationToken ct = default) → Task<bool>` to `Domain/Interfaces/Repositories/IUserSavedPresetRepository.cs`
- [ ] T038 Implement `ExistsByNameAsync` in `Repository/Repositories/UserSavedPresetRepository.cs` — `context.UserSavedPresets.Where(p => p.UserId == userId && p.Name == name && (excludePresetId == null || p.Id != excludePresetId)).AnyAsync(ct)` (depends on T037)
- [ ] T039 [US4] Implement `UserService.GetPresetsAsync(Guid userId, CancellationToken ct)` in `Domain/Services/UserService.cs` — call `_presetRepo.GetByUserIdAsync(userId, ct)`; return list (already ordered by caller — note: add `.OrderBy(p => p.Name)` in repository or service)
- [ ] T040 [US4] Implement `UserService.CreatePresetAsync(Guid userId, string name, string stylesJson, CancellationToken ct)` in `Domain/Services/UserService.cs` — check `_presetRepo.ExistsByNameAsync(userId, name, ct: ct)` → throw `ConflictException("DUPLICATE_PRESET_NAME", ...)` if true; create `UserSavedPreset { Id = Guid.NewGuid(), UserId = userId, Name = name, StylesJson = stylesJson }`; call `_presetRepo.AddAsync(preset, ct)`; call `_uow.CommitAsync(ct)`; return preset (depends on T037, T038)
- [ ] T041 [US4] Implement `UserService.UpdatePresetAsync(Guid userId, Guid presetId, string? name, string? stylesJson, CancellationToken ct)` in `Domain/Services/UserService.cs` — `GetByIdAsync(presetId)` → `NotFoundException` if null; `preset.UserId != userId` → `ForbiddenException`; if name provided and differs from current: check `ExistsByNameAsync(userId, name, excludePresetId: presetId)` → `ConflictException("DUPLICATE_PRESET_NAME", ...)` if true; apply changes; `_uow.CommitAsync(ct)`; return preset (depends on T037, T038)
- [ ] T042 [US4] Implement `UserService.DeletePresetAsync(Guid userId, Guid presetId, CancellationToken ct)` in `Domain/Services/UserService.cs` — load preset; `NotFoundException` if null; `ForbiddenException` if not owned; `_presetRepo.Remove(preset)`; `_uow.CommitAsync(ct)`
- [ ] T043 [US4] Add `GetPresets` action to `Api/Controllers/UsersController.cs` — `[HttpGet("me/presets")]` returning `Ok(_mapper.Map<List<PresetResponse>>(await _userService.GetPresetsAsync(GetUserId(), ct)))`
- [ ] T044 [US4] Add `CreatePreset` action to `Api/Controllers/UsersController.cs` — `[HttpPost("me/presets")]` accepting `SavePresetRequest request`; serialize `request.Styles` to JSON; call `CreatePresetAsync`; return `StatusCode(201, _mapper.Map<PresetResponse>(preset))`
- [ ] T045 [US4] Add `UpdatePreset` action to `Api/Controllers/UsersController.cs` — `[HttpPut("me/presets/{id:guid}")]` accepting `UpdatePresetRequest request`; serialize `request.Styles` to JSON if not null; call `UpdatePresetAsync`; return `Ok(_mapper.Map<PresetResponse>(preset))`
- [ ] T046 [US4] Add `DeletePreset` action to `Api/Controllers/UsersController.cs` — `[HttpDelete("me/presets/{id:guid}")]` calling `await _userService.DeletePresetAsync(GetUserId(), id, ct)`; return `NoContent()`

**Checkpoint**: All preset CRUD works. Duplicate name returns 409. Cross-user access returns 403. List is alphabetically sorted.

---

## Phase 7: User Story 5 — Automatic Account Deletion Cleanup (Priority: P3)

**Goal**: A daily background service hard-deletes all accounts whose 30-day grace period has expired, including their avatar blobs.

**Independent Test**: Seed a user with `ScheduledDeletionAt = DateTime.UtcNow.AddDays(-1)`, trigger the service, verify user row and all cascade data are gone and avatar blob is deleted.

- [ ] T047 Add `GetExpiredForDeletionAsync(CancellationToken ct = default) → Task<IReadOnlyList<User>>` to `Domain/Interfaces/Repositories/IUserRepository.cs`
- [ ] T048 Implement `GetExpiredForDeletionAsync` in `Repository/Repositories/UserRepository.cs` — `context.Users.Where(u => u.ScheduledDeletionAt != null && u.ScheduledDeletionAt <= DateTime.UtcNow).ToListAsync(ct)` then `mapper.Map<List<User>>(entities)` (depends on T047)
- [ ] T049 [US5] Create `Application/BackgroundServices/AccountDeletionCleanupService.cs` — extend `BackgroundService`; constructor injects `IServiceScopeFactory` (for scoped repos) and `IAzureBlobService` (singleton) and `ILogger<AccountDeletionCleanupService>`; `ExecuteAsync` uses `PeriodicTimer(TimeSpan.FromHours(24))` loop; each tick creates a scope, resolves `IUserRepository` + `IUnitOfWork`, calls `GetExpiredForDeletionAsync`, iterates users: try `_blobService.DeleteAsync($"avatars/{u.Id}", ct)` if `AvatarUrl != null` (log warning on failure, continue — `DeleteAsync` calls `DeleteBlobIfExistsAsync` so a missing blob is a no-op), then `userRepo.Remove(u)`; after loop call `uow.CommitAsync(ct)` once in a try/catch — if commit fails, log the error and return without rethrowing (accounts retain their `ScheduledDeletionAt` and will be re-selected on the next daily run); wrap per-user blob deletion in a separate try/catch so a single blob failure does not prevent the remaining removes; note: class is already registered in `AddBackgroundWorkers()` — no DI change needed (depends on T047, T048, T008)

**Checkpoint**: Seeded expired accounts are cleaned up on next timer tick. Active accounts are untouched. Blob failures are logged and do not abort the batch.

---

## Phase 8: Polish & Tests

**Purpose**: Mandatory test coverage per constitution (Principle VIII). Unit tests for service logic; integration tests for all 10 HTTP endpoints.

- [ ] T050 [P] Create `Tests/Unit/Services/UserServiceTests.cs` — one test class using Moq for all `IUserService` dependencies; cover: `ScheduleDeletion_WhenAlreadyScheduled_ThrowsConflictException`, `ScheduleDeletion_WhenNotScheduled_SetsScheduledDeletionAt30DaysOut`, `CancelDeletion_WhenNotScheduled_ThrowsBadRequestException`, `CancelDeletion_WhenScheduled_ClearsScheduledDeletionAt`, `UploadAvatar_WhenExistingAvatar_DeletesOldBlobFirst`, `DeleteAvatar_WhenNoAvatar_ReturnsWithoutError`, `UpdateProfile_WhenInstrumentNotFound_ThrowsNotFoundException`, `CreatePreset_WhenDuplicateName_ThrowsConflictException`, `UpdatePreset_WhenNotOwner_ThrowsForbiddenException`, `UpdatePreset_WhenNotFound_ThrowsNotFoundException`, `UpdatePreset_WhenRenameToSameName_Succeeds`
- [ ] T051 [P] Create `Tests/Integration/Controllers/UsersControllerTests.cs` — `WebApplicationFactory<Program>` with InMemory EF; inject test JWT via `AuthHelper`; one test per endpoint covering: `GET /users/me` (200 + correct shape), `PUT /users/me` (200 + persisted), `PUT /users/me` with missing field (400), `DELETE /users/me` (204 + scheduledDeletionAt set), `DELETE /users/me` when already scheduled (409), `POST /users/me/cancel-deletion` (204), `POST /users/me/cancel-deletion` when not scheduled (400), `GET /users/me/presets` (200 + empty array), `POST /users/me/presets` (201), `POST /users/me/presets` duplicate name (409), `PUT /users/me/presets/{id}` (200), `PUT /users/me/presets/{id}` other user (403), `DELETE /users/me/presets/{id}` (204)
- [ ] T052 Run `dotnet test Staccato.sln` — confirm all existing tests still pass and all new tests pass

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup)         → no dependencies
Phase 2 (Foundational)  → depends on Phase 1 — BLOCKS all user stories
Phase 3 (US1 Profile)   → depends on Phase 2
Phase 4 (US2 Deletion)  → depends on Phase 2 (and Phase 3 for UserService class file)
Phase 5 (US3 Avatar)    → depends on Phase 2 (and Phase 3 for UserService class file)
Phase 6 (US4 Presets)   → depends on Phase 2 (and Phase 3 for UserService class file)
Phase 7 (US5 Cleanup)   → depends on Phase 2, Phase 5 (reuses IAzureBlobService)
Phase 8 (Tests)         → depends on all preceding phases
```

### User Story Dependencies

- **US1 (P1)**: No dependency on other user stories. Start immediately after Phase 2.
- **US2 (P1)**: `UserService.cs` file created in US1 phase — implement deletion methods in same file. Can begin after Phase 2.
- **US3 (P2)**: Depends on `IAzureBlobService` (Phase 2). Can begin after Phase 2.
- **US4 (P2)**: Depends on `UserSavedPresetRepository` (pre-existing). Can begin after Phase 2.
- **US5 (P3)**: Depends on `IAzureBlobService` (Phase 2) and `IUserRepository.GetExpiredForDeletionAsync`. No dependency on US1–US4.

### Within Each Phase

- `[P]` tasks within a phase may be started together (different files)
- Non-`[P]` tasks within a phase must run sequentially (same file edits or dependencies)

### Parallel Opportunities

```bash
# Phase 2 — run in parallel after T003, T006, T007:
T004  Add DefaultPageSize/DefaultInstrumentId to UserEntity
T005  Add fields to User domain model
T008  Create IAzureBlobService interface
T010  Create IUserService interface

# Phase 3 — run in parallel:
T014  Create UserResponse.cs
T015  Create UpdateProfileRequest.cs

# Phase 6 — run in parallel:
T030  Create StyleEntryDto.cs
T031  Create PresetResponse.cs
T032  Create SavePresetRequest.cs
T034  Create UpdatePresetRequest.cs

# Phase 8 — run in parallel:
T050  UserServiceTests.cs
T051  UsersControllerTests.cs
```

---

## Implementation Strategy

### MVP First (US1 + US2 only — both P1)

1. Complete Phase 1 + Phase 2 (foundational)
2. Complete Phase 3 (US1 — profile endpoints)
3. Complete Phase 4 (US2 — deletion lifecycle)
4. **STOP and VALIDATE**: `GET /users/me`, `PUT /users/me`, `DELETE /users/me`, `POST /users/me/cancel-deletion` all functional
5. Proceed to P2 stories (avatar, presets) and P3 (cleanup)

### Incremental Delivery

```
Phase 1+2 → Foundation
Phase 3   → Profile endpoints live (MVP slice 1)
Phase 4   → Deletion lifecycle live (MVP slice 2 — full P1 done)
Phase 5   → Avatar upload live
Phase 6   → Style presets live
Phase 7   → Background cleanup live
Phase 8   → Full test suite passing
```

---

## Notes

- `[P]` tasks touch different files — safe to parallelize
- `UserService.cs` is edited across Phases 3–7 (not parallelizable across phases for a single developer)
- `UsersController.cs` is edited across Phases 3–7 (add actions incrementally per story)
- `AccountDeletionCleanupService` is already registered in `AddBackgroundWorkers()` — T049 only creates the class file
- `DomainToResponseProfile.cs` is created in Phase 3 (T017) and extended in Phase 6 (T036)
- Commit after each checkpoint to maintain a clean git history per feature slice
