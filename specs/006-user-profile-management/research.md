# Research: User Profile Management

**Feature**: 006-user-profile-management
**Date**: 2026-03-28

---

## Decision 1: AzureBlobService Placement

**Decision**: Implement `AzureBlobService` in `Application/Services/`, not `Persistence/Services/`.

**Rationale**: `Azure.Storage.Blobs` and the `BlobServiceClient` singleton are already registered in the Application project (`ServiceCollectionExtensions.AddAzureBlob()`). The `Application.Services` namespace is already imported in `ServiceCollectionExtensions.cs`. Placing the implementation there requires zero new project references. `Persistence` only depends on `EntityModels` — adding Azure SDK to Persistence would introduce an infrastructure dependency that violates its minimal footprint.

**Alternatives considered**:
- `Persistence/Services/AzureBlobService.cs`: Would require adding `Azure.Storage.Blobs` to `Persistence.csproj`, polluting the EF-only layer.
- New `Infrastructure` project: Would exceed the 9-project limit mandated by the constitution.

---

## Decision 2: IAzureBlobService Contract

**Decision**: Three-method interface — `UploadAsync` returns the full public blob URL; `DeleteAsync` is idempotent (no error on missing blob); `GetStreamAsync` returns a nullable stream.

```csharp
Task<string> UploadAsync(Stream content, string contentType, string blobPath, CancellationToken ct = default);
Task DeleteAsync(string blobPath, CancellationToken ct = default);
Task<Stream?> GetStreamAsync(string blobPath, CancellationToken ct = default);
```

**Rationale**: `UploadAsync` returns the full public URL so `UserService` can store it directly in `User.AvatarUrl` without re-constructing the URL. `DeleteAsync` is idempotent because blob may already be absent (e.g., failed prior upload, or concurrent cleanup) — callers should not need to check existence first. `GetStreamAsync` is included for future use by the PDF proxy download endpoint.

**Alternatives considered**:
- Returning `void` from `UploadAsync` and reconstructing the URL in UserService: Would require injecting `IOptions<AzureBlobOptions>` into `UserService` (a Domain service), which must not reference configuration directly.
- Throwing on missing blob in `DeleteAsync`: Complicates cleanup service error handling for no benefit.

---

## Decision 3: displayName → firstName + lastName

**Decision**: `UserResponse` exposes `firstName` and `lastName` as separate fields. `UpdateProfileRequest` accepts `firstName` and `lastName` separately. The spec's `displayName` maps to these two fields.

**Rationale**: `UserEntity.FirstName` and `UserEntity.LastName` were established in feature 005. `AuthService.RegisterAsync` already splits a single `displayName` string on the first space character. Exposing the two fields separately is strictly more precise — roundtripping "Mary Jane Watson" through a split would incorrectly parse `firstName="Mary"`, `lastName="Jane Watson"`, while first/last fields keep names unambiguous. Frontend consumers can concatenate for display.

**Alternatives considered**:
- Single `displayName` with split in `UserService`: Lossy operation; "Mary Jane" and "Mary-Jane" produce different entity values.
- Add a separate `DisplayName` column: Redundant storage alongside existing columns; would break consistency with auth registration.

---

## Decision 4: AccountDeletionCleanupService Timer Pattern

**Decision**: Use a `PeriodicTimer` with a 24-hour period inside `ExecuteAsync`. Resolve scoped dependencies (`IUserRepository`, `IUnitOfWork`) via `IServiceScopeFactory` per iteration. Inject `IAzureBlobService` directly (singleton lifetime).

**Rationale**: `PeriodicTimer` (introduced in .NET 6) is the idiomatic pattern for recurring background work — it avoids drift, supports cancellation, and is cleaner than `Task.Delay` loops. Scoped repositories must be resolved per-scope to avoid EF Core DbContext lifetime issues. `AccountDeletionCleanupService` is already registered in `AddBackgroundWorkers()` — only the class file needs to be created.

**Alternatives considered**:
- `Task.Delay(TimeSpan.FromHours(24), ct)` loop: Works but can drift; PeriodicTimer is the preferred modern approach.
- External scheduler (Hangfire, Quartz): Prohibited by constitution (no new major library).
- Running on startup then daily: Starting immediately on first boot could cause deletions before the system is fully ready. First tick fires after the initial 24h wait.

---

## Decision 5: Preset Styles Storage Format

**Decision**: `UserSavedPreset.StylesJson` stores a JSON array of `{ moduleType, stylesJson }` objects — one element per `ModuleType` enum value (12 total). `IAzureBlobService` is not involved. The API accepts and returns this structure via `StyleEntryDto`.

**Rationale**: `UserSavedPresetEntity.StylesJson` already exists as a single JSON column (established in feature 003 migration). This mirrors the `NotebookModuleStyleEntity.StylesJson` pattern where each entry's visual config is an opaque JSON blob managed by the frontend. The backend validates structural integrity (count == 12, all ModuleTypes present, no duplicates) without parsing the nested `stylesJson` content.

**Alternatives considered**:
- Separate `UserSavedPresetStyleEntity` table (one row per module type per preset): Normalised but adds 12× row count, extra join, and more migration complexity for no query benefit.
- Strongly-typed style properties in the entity: Would couple the backend to the frontend's visual schema; the schema is owned by the frontend and can evolve independently.
