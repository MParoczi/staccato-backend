# Quickstart: User Profile Management

**Feature**: 006-user-profile-management
**Branch**: `006-user-profile-management`

Implement in the order listed below. Each step builds on the previous and is independently compilable.

---

## Step 1 — Domain exceptions (5 min)

Create `Domain/Exceptions/BadRequestException.cs`:

```csharp
namespace Domain.Exceptions;

public class BadRequestException : BusinessException
{
    public BadRequestException(string code, string message, object? details = null)
        : base(code, message, details)
    {
        StatusCode = 400;
    }
}
```

---

## Step 2 — Extend entity and domain models (10 min)

**EntityModels/Entities/UserEntity.cs** — add after `Language`:
```csharp
public PageSize? DefaultPageSize { get; set; }
public Guid? DefaultInstrumentId { get; set; }
public InstrumentEntity? DefaultInstrument { get; set; }
```

**DomainModels/Models/User.cs** — add:
```csharp
public PageSize? DefaultPageSize { get; set; }
public Guid? DefaultInstrumentId { get; set; }
```

---

## Step 3 — EF configuration + migration (10 min)

**Persistence/Configurations/UserConfiguration.cs** — add inside `Configure`:
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

Run migration:
```bash
dotnet ef migrations add AddUserPreferences \
  --project Persistence/Persistence.csproj \
  --startup-project Application/Application.csproj
```

---

## Step 4 — Extend repository interfaces (5 min)

**IUserSavedPresetRepository.cs** — add:
```csharp
Task<bool> ExistsByNameAsync(Guid userId, string name, Guid? excludePresetId = null, CancellationToken ct = default);
```

**IUserRepository.cs** — add:
```csharp
Task<IReadOnlyList<User>> GetExpiredForDeletionAsync(CancellationToken ct = default);
```

---

## Step 5 — Implement repository methods (15 min)

**UserSavedPresetRepository.cs**:
```csharp
public async Task<bool> ExistsByNameAsync(Guid userId, string name, Guid? excludePresetId = null, CancellationToken ct = default)
    => await context.UserSavedPresets
        .Where(p => p.UserId == userId
                 && p.Name == name
                 && (excludePresetId == null || p.Id != excludePresetId))
        .AnyAsync(ct);
```

**UserRepository.cs**:
```csharp
public async Task<IReadOnlyList<User>> GetExpiredForDeletionAsync(CancellationToken ct = default)
{
    var now = DateTime.UtcNow;
    var entities = await context.Users
        .Where(u => u.ScheduledDeletionAt != null && u.ScheduledDeletionAt <= now)
        .ToListAsync(ct);
    return mapper.Map<List<User>>(entities);
}
```

---

## Step 6 — IAzureBlobService + AzureBlobService (15 min)

**Domain/Services/IAzureBlobService.cs**:
```csharp
namespace Domain.Services;

public interface IAzureBlobService
{
    Task<string> UploadAsync(Stream content, string contentType, string blobPath, CancellationToken ct = default);
    Task DeleteAsync(string blobPath, CancellationToken ct = default);
    Task<Stream?> GetStreamAsync(string blobPath, CancellationToken ct = default);
}
```

**Application/Services/AzureBlobService.cs** — inject `BlobServiceClient` and `IOptions<AzureBlobOptions>`. `UploadAsync` calls `GetBlobClient(blobPath).UploadAsync(content, overwrite: true)` and returns the full URI (`blobClient.Uri.ToString()`). `DeleteAsync` calls `DeleteIfExistsAsync`. `GetStreamAsync` calls `DownloadStreamingAsync` and returns the stream (null if not found).

Register in `ServiceCollectionExtensions.AddDomainServices()`:
```csharp
services.AddSingleton<IAzureBlobService, AzureBlobService>();
services.AddScoped<IUserService, UserService>();
```

---

## Step 7 — IUserService + UserService (45 min)

**Domain/Services/IUserService.cs** — define 10 methods matching the service method map in `contracts/api.md`.

**Domain/Services/UserService.cs** — inject `IUserRepository`, `IUserSavedPresetRepository`, `IInstrumentRepository`, `IAzureBlobService`, `IUnitOfWork`.

Key business rules to enforce in UserService:
- `ScheduleDeletionAsync`: if `user.ScheduledDeletionAt != null` → throw `ConflictException("ACCOUNT_DELETION_ALREADY_SCHEDULED", ...)`
- `CancelDeletionAsync`: if `user.ScheduledDeletionAt == null` → throw `BadRequestException("ACCOUNT_DELETION_NOT_SCHEDULED", ...)`
- `UpdateProfileAsync`: if `defaultInstrumentId != null` and instrument not found → throw `NotFoundException`
- `UploadAvatarAsync`: if existing avatar URL → delete old blob first; upload new; update `user.AvatarUrl`; commit
- `DeleteAvatarAsync`: if `user.AvatarUrl == null` → return (no-op); otherwise delete blob, null out URL, commit
- `CreatePresetAsync`: check `ExistsByNameAsync(userId, name)` → throw `ConflictException("DUPLICATE_PRESET_NAME", ...)` if true
- `UpdatePresetAsync`: `GetByIdAsync(presetId)` → null = `NotFoundException`; ownership check = `ForbiddenException`; if name changed, check uniqueness excluding self
- `DeletePresetAsync`: same ownership pattern, then `_presetRepo.Remove(preset)` + commit

---

## Step 8 — ApiModels (20 min)

Create in `ApiModels/Users/`:

**UpdateProfileRequest.cs** — record or class with `FirstName`, `LastName`, `Language`, `DefaultPageSize?`, `DefaultInstrumentId?`.

**UpdateProfileRequestValidator.cs** — `RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100)`. `RuleFor(x => x.LastName).MaximumLength(100)`. `RuleFor(x => x.Language).IsInEnum()`. `RuleFor(x => x.DefaultPageSize).IsInEnum().When(x => x.DefaultPageSize.HasValue)`.

**StyleEntryDto.cs** — record with `ModuleType` (string) and `StylesJson` (string).

**SavePresetRequest.cs** — record with `Name` (string) and `Styles` (`IList<StyleEntryDto>`).

**SavePresetRequestValidator.cs** — name: `NotEmpty().MaximumLength(100)`. Styles: `NotNull().Must(s => s.Count == 12).WithMessage("Exactly 12 style entries required.")`. Custom rule for no duplicate moduleType values and each is a valid `ModuleType` enum name.

**UpdatePresetRequest.cs** — record with `Name?` and `Styles?`. Validator: at least one field non-null; if name provided: `NotEmpty().MaximumLength(100)`; if styles provided: same 12-entry validation.

**UserResponse.cs** and **PresetResponse.cs** — simple record shapes matching `contracts/api.md`.

---

## Step 9 — AutoMapper profile (10 min)

Create `Api/Mapping/DomainToResponseProfile.cs`:
```csharp
namespace Api.Mapping;

public class DomainToResponseProfile : Profile
{
    public DomainToResponseProfile()
    {
        CreateMap<User, UserResponse>();
        CreateMap<UserSavedPreset, PresetResponse>()
            .ForMember(d => d.Styles,
                o => o.MapFrom(s => JsonSerializer.Deserialize<List<StyleEntryDto>>(s.StylesJson)));
    }
}
```

---

## Step 10 — UsersController (20 min)

Create `Api/Controllers/UsersController.cs`. Route prefix: `users`. All actions: `[Authorize]`. Extract userId:
```csharp
var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
```

Avatar upload action receives `IFormFile file` from the request. Delegate size/MIME validation to `UploadAvatarRequestValidator` (or validate inline if simpler — file constraints are infrastructure-level). Call `_userService.UploadAvatarAsync(userId, file.OpenReadStream(), file.ContentType, ct)`.

Map results with AutoMapper `IMapper`. Return shapes from `contracts/api.md`.

---

## Step 11 — AccountDeletionCleanupService (20 min)

Create `Application/BackgroundServices/AccountDeletionCleanupService.cs` extending `BackgroundService`. Already registered in `AddBackgroundWorkers()`.

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
    while (await timer.WaitForNextTickAsync(stoppingToken))
    {
        await RunCleanupAsync(stoppingToken);
    }
}
```

`RunCleanupAsync`: create scope via `IServiceScopeFactory`, resolve `IUserRepository` and `IUnitOfWork`. Call `GetExpiredForDeletionAsync`. For each user: try `_blobService.DeleteAsync(avatarUrl)` if AvatarUrl is not null (log warning on failure, continue), then `_userRepo.Remove(user)`. After loop: `_unitOfWork.CommitAsync` once to batch all deletes. Wrap per-user blob deletion in try/catch to avoid halting the batch.

Inject `IAzureBlobService` directly (singleton) and `IServiceScopeFactory` (singleton). Do not inject scoped services into the constructor.

---

## Step 12 — Tests (30 min)

**Tests/Unit/Services/UserServiceTests.cs**:
- `ScheduleDeletion_WhenAlreadyScheduled_ThrowsConflictException`
- `ScheduleDeletion_WhenNotScheduled_SetsScheduledDeletionAt`
- `CancelDeletion_WhenNotScheduled_ThrowsBadRequestException`
- `CancelDeletion_WhenScheduled_ClearsScheduledDeletionAt`
- `UploadAvatar_DeletesOldBlobFirst_WhenExistingAvatarPresent`
- `DeleteAvatar_WhenNoAvatar_ReturnsWithoutError`
- `CreatePreset_WhenDuplicateName_ThrowsConflictException`
- `UpdatePreset_WhenNotOwner_ThrowsForbiddenException`
- `UpdatePreset_WhenNotFound_ThrowsNotFoundException`

**Tests/Integration/Controllers/UsersControllerTests.cs**: one test class per endpoint group (profile, deletion, avatar, presets), using `WebApplicationFactory` with InMemory EF and `AuthHelper` for JWT injection.

---

## Build & verify

```bash
dotnet build Staccato.sln
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~Unit"
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~Integration"
```
