# Data Model: User Profile Management

**Feature**: 006-user-profile-management
**Date**: 2026-03-28

---

## Entities Changed

### UserEntity (modified)

**File**: `EntityModels/Entities/UserEntity.cs`

Two nullable preference columns added. Both are optional — null means "no preference set."

| Field | Type | Nullable | Notes |
|---|---|---|---|
| `DefaultPageSize` | `PageSize` (enum) | ✅ | User's preferred notebook page size. FK-free; stored as enum int. |
| `DefaultInstrumentId` | `Guid` | ✅ | FK → `InstrumentEntity.Id` with `DeleteBehavior.Restrict`. |
| `DefaultInstrument` | `InstrumentEntity?` | ✅ | Navigation property (not mapped to column). |

All existing fields (`Id`, `Email`, `PasswordHash`, `GoogleId`, `FirstName`, `LastName`, `AvatarUrl`, `CreatedAt`, `ScheduledDeletionAt`, `Language`) are unchanged.

### User domain model (modified)

**File**: `DomainModels/Models/User.cs`

Mirror of entity changes:
- `PageSize? DefaultPageSize`
- `Guid? DefaultInstrumentId`

No navigation properties in domain models (DomainModels has zero dependencies).

---

## Entities Unchanged (Pre-existing)

### UserSavedPresetEntity

**File**: `EntityModels/Entities/UserSavedPresetEntity.cs` — no changes needed.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK, app-generated |
| `UserId` | `Guid` | FK → `UserEntity.Id`, cascade delete |
| `Name` | `string` | Max 100 chars, unique per user (enforced in service layer) |
| `StylesJson` | `string` | JSON array of `{ moduleType, stylesJson }` — 12 entries |
| `User` | `UserEntity` | Navigation property |

### UserSavedPreset domain model

**File**: `DomainModels/Models/UserSavedPreset.cs` — no changes needed.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | |
| `UserId` | `Guid` | |
| `Name` | `string` | |
| `StylesJson` | `string` | Opaque JSON — parsed only for structural validation |

---

## StylesJson Schema

`UserSavedPreset.StylesJson` is a JSON array with exactly 12 elements, one per `ModuleType` enum value. Each element:

```jsonc
{
  "moduleType": "Title",      // string — ModuleType enum name
  "stylesJson": "{...}"       // string — opaque style config managed by frontend
}
```

Structural validation (performed in `SavePresetRequestValidator`):
1. Array length must equal 12
2. Each `moduleType` must be a valid `ModuleType` enum name
3. No duplicate `moduleType` values
4. `stylesJson` must be a non-empty string

The `stylesJson` inner content is **not** further validated by the backend — it is frontend-owned.

---

## Repository Interface Changes

### IUserSavedPresetRepository (extended)

**File**: `Domain/Interfaces/Repositories/IUserSavedPresetRepository.cs`

New method added:

```csharp
/// <summary>
///     Returns true if a preset with the given name already exists for the user.
///     Pass excludePresetId to skip the current preset during rename operations.
/// </summary>
Task<bool> ExistsByNameAsync(
    Guid userId,
    string name,
    Guid? excludePresetId = null,
    CancellationToken ct = default);
```

Existing method unchanged: `GetByUserIdAsync(Guid userId, CancellationToken ct)`.

### IUserRepository (extended)

**File**: `Domain/Interfaces/Repositories/IUserRepository.cs`

New method added:

```csharp
/// <summary>
///     Returns all users whose ScheduledDeletionAt is not null and is <= DateTime.UtcNow.
///     Used by AccountDeletionCleanupService.
/// </summary>
Task<IReadOnlyList<User>> GetExpiredForDeletionAsync(CancellationToken ct = default);
```

---

## EF Configuration Changes

### UserConfiguration (modified)

**File**: `Persistence/Configurations/UserConfiguration.cs`

Additions:

```csharp
builder.Property(u => u.DefaultPageSize)
    .HasConversion<string>()
    .HasColumnType("nvarchar(50)");

builder.HasOne<InstrumentEntity>()
    .WithMany()
    .HasForeignKey(u => u.DefaultInstrumentId)
    .IsRequired(false)
    .OnDelete(DeleteBehavior.Restrict);
```

---

## Migration

**Name**: `AddUserPreferences`

**Command** (run from solution root):
```bash
dotnet ef migrations add AddUserPreferences \
  --project Persistence/Persistence.csproj \
  --startup-project Application/Application.csproj
```

**Generated DDL (expected)**:
```sql
ALTER TABLE [Users] ADD [DefaultPageSize] nvarchar(50) NULL;
ALTER TABLE [Users] ADD [DefaultInstrumentId] uniqueidentifier NULL;

ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Instruments_DefaultInstrumentId]
  FOREIGN KEY ([DefaultInstrumentId]) REFERENCES [Instruments] ([Id])
  ON DELETE NO ACTION;  -- Restrict maps to NO ACTION in SQL Server
```

---

## State Transitions

### User.ScheduledDeletionAt

```
null  ──[DELETE /users/me]──►  DateTime.UtcNow + 30 days
                               │
                               ├─[POST /users/me/cancel-deletion]──► null
                               │
                               └─[AccountDeletionCleanupService, when <=UtcNow]──► (row deleted)
```

### User.AvatarUrl

```
null  ──[PUT /users/me/avatar (valid upload)]──►  full public blob URL
                                                  │
full URL  ──[PUT /users/me/avatar (new upload)]──► new full public blob URL
                                                   (old blob deleted first)
                                                  │
full URL  ──[DELETE /users/me/avatar]──► null  (blob deleted)
null  ──[DELETE /users/me/avatar]──► null      (no-op, 204)
```
