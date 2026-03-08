# Domain Interface Contracts

**Branch**: `004-repository-uow` | **Date**: 2026-03-08

These are the interfaces to be created in the `Domain` project. They contain zero implementation detail — only method signatures and behavioural contracts.

---

## Domain/Interfaces/IUnitOfWork.cs

```csharp
namespace Domain.Interfaces;

public interface IUnitOfWork
{
    /// <summary>
    /// Flushes all staged repository changes to the database as one atomic transaction.
    /// Returns the number of state entries written.
    /// </summary>
    Task<int> CommitAsync(CancellationToken ct = default);
}
```

---

## Domain/Interfaces/Repositories/IRepository.cs

```csharp
namespace Domain.Interfaces.Repositories;

public interface IRepository<T>
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Remove(T entity);
    void Update(T entity);
}
```

---

## Domain/Interfaces/Repositories/IUserRepository.cs

```csharp
using DomainModels.Models;

namespace Domain.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken ct = default);

    /// <summary>
    /// Returns the user and their currently active (non-revoked, non-expired) refresh tokens.
    /// Returns null if no user with the given ID exists.
    /// </summary>
    Task<(User User, IReadOnlyList<RefreshToken> Tokens)?> GetWithActiveTokensAsync(
        Guid userId, CancellationToken ct = default);
}
```

---

## Domain/Interfaces/Repositories/INotebookRepository.cs

```csharp
using DomainModels.Models;

namespace Domain.Interfaces.Repositories;

public interface INotebookRepository : IRepository<Notebook>
{
    Task<IReadOnlyList<Notebook>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns the notebook and all 12 of its module styles.
    /// Returns null if no notebook with the given ID exists.
    /// </summary>
    Task<(Notebook Notebook, IReadOnlyList<NotebookModuleStyle> Styles)?> GetWithStylesAsync(
        Guid notebookId, CancellationToken ct = default);
}
```

---

## Domain/Interfaces/Repositories/ILessonRepository.cs

```csharp
using DomainModels.Models;

namespace Domain.Interfaces.Repositories;

public interface ILessonRepository : IRepository<Lesson>
{
    /// <summary>
    /// Returns all lessons for the notebook ordered by CreatedAt ascending.
    /// Returns an empty list when the notebook has no lessons.
    /// </summary>
    Task<IReadOnlyList<Lesson>> GetByNotebookIdOrderedByCreatedAtAsync(
        Guid notebookId, CancellationToken ct = default);

    /// <summary>
    /// Returns the lesson and its pages ordered by PageNumber ascending.
    /// Returns null if no lesson with the given ID exists.
    /// </summary>
    Task<(Lesson Lesson, IReadOnlyList<LessonPage> Pages)?> GetWithPagesAsync(
        Guid lessonId, CancellationToken ct = default);
}
```

---

## Domain/Interfaces/Repositories/ILessonPageRepository.cs

```csharp
using DomainModels.Models;

namespace Domain.Interfaces.Repositories;

public interface ILessonPageRepository : IRepository<LessonPage>
{
    /// <summary>
    /// Returns all pages for the lesson ordered by PageNumber ascending.
    /// Returns an empty list when the lesson has no pages.
    /// </summary>
    Task<IReadOnlyList<LessonPage>> GetByLessonIdOrderedAsync(
        Guid lessonId, CancellationToken ct = default);

    /// <summary>
    /// Returns the page and all its modules.
    /// Returns null if no page with the given ID exists.
    /// </summary>
    Task<(LessonPage Page, IReadOnlyList<Module> Modules)?> GetPageWithModulesAsync(
        Guid pageId, CancellationToken ct = default);
}
```

---

## Domain/Interfaces/Repositories/IModuleRepository.cs

```csharp
using DomainModels.Models;

namespace Domain.Interfaces.Repositories;

public interface IModuleRepository : IRepository<Module>
{
    Task<IReadOnlyList<Module>> GetByPageIdAsync(Guid pageId, CancellationToken ct = default);

    /// <summary>
    /// Returns true if the proposed rectangle overlaps any existing module on the page.
    /// When excludeModuleId is provided, that module is excluded from the check (update scenario).
    /// Returns false when the page has no modules (or only the excluded module).
    /// </summary>
    Task<bool> CheckOverlapAsync(
        Guid pageId,
        int gridX, int gridY, int gridWidth, int gridHeight,
        Guid? excludeModuleId = null,
        CancellationToken ct = default);
}
```

---

## Domain/Interfaces/Repositories/IChordRepository.cs

```csharp
using DomainModels.Models;

namespace Domain.Interfaces.Repositories;

public interface IChordRepository : IRepository<Chord>
{
    /// <summary>
    /// Returns chords matching the given instrument and optional filters.
    /// Null root or quality means "no filter on that dimension".
    /// </summary>
    Task<IReadOnlyList<Chord>> SearchAsync(
        Guid instrumentId,
        string? root,
        string? quality,
        CancellationToken ct = default);
}
```

---

## Domain/Interfaces/Repositories/IInstrumentRepository.cs

```csharp
using DomainModels.Models;

namespace Domain.Interfaces.Repositories;

public interface IInstrumentRepository : IRepository<Instrument>
{
    /// <summary>
    /// Returns all instruments ordered by Name ascending.
    /// </summary>
    Task<IReadOnlyList<Instrument>> GetAllAsync(CancellationToken ct = default);
}
```

---

## Domain/Interfaces/Repositories/IPdfExportRepository.cs

```csharp
using DomainModels.Models;

namespace Domain.Interfaces.Repositories;

public interface IPdfExportRepository : IRepository<PdfExport>
{
    /// <summary>
    /// Returns the active export (Pending, Processing, or Ready) for the given notebook,
    /// or null if none exists.
    /// </summary>
    Task<PdfExport?> GetActiveExportForNotebookAsync(Guid notebookId, CancellationToken ct = default);

    /// <summary>
    /// Returns all non-Failed exports whose CreatedAt is strictly older than utcCutoff.
    /// The repository must NOT call DateTime.UtcNow internally.
    /// </summary>
    Task<IReadOnlyList<PdfExport>> GetExpiredExportsAsync(DateTime utcCutoff, CancellationToken ct = default);

    /// <summary>
    /// Returns all exports for the user ordered by CreatedAt descending.
    /// </summary>
    Task<IReadOnlyList<PdfExport>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
}
```

---

## Domain/Interfaces/Repositories/IRefreshTokenRepository.cs

```csharp
using DomainModels.Models;

namespace Domain.Interfaces.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Returns all non-revoked, non-expired tokens for the user.
    /// </summary>
    Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Bulk-revokes all refresh tokens for the user via a single SQL UPDATE.
    /// NOTE: This call commits immediately and does NOT participate in IUnitOfWork.
    /// Callers MUST NOT call IUnitOfWork.CommitAsync for this operation — the revocation
    /// is already persisted when this method returns.
    /// Use for logout-all-devices and account deletion flows only.
    /// </summary>
    Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default);
}
```

---

## Domain/Interfaces/Repositories/INotebookModuleStyleRepository.cs

```csharp
using DomainModels.Enums;
using DomainModels.Models;

namespace Domain.Interfaces.Repositories;

public interface INotebookModuleStyleRepository : IRepository<NotebookModuleStyle>
{
    /// <summary>
    /// Returns all 12 styles for the notebook ordered by ModuleType (enum integer value ascending).
    /// </summary>
    Task<IReadOnlyList<NotebookModuleStyle>> GetByNotebookIdAsync(
        Guid notebookId, CancellationToken ct = default);

    Task<NotebookModuleStyle?> GetByNotebookIdAndTypeAsync(
        Guid notebookId, ModuleType moduleType, CancellationToken ct = default);
}
```

---

## Domain/Interfaces/Repositories/IUserSavedPresetRepository.cs

```csharp
using DomainModels.Models;

namespace Domain.Interfaces.Repositories;

public interface IUserSavedPresetRepository : IRepository<UserSavedPreset>
{
    /// <summary>
    /// Returns all saved presets for the user. Returns an empty list when none exist.
    /// </summary>
    Task<IReadOnlyList<UserSavedPreset>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
}
```
