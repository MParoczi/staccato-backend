using Domain.Exceptions;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using DomainModels.Models;

namespace Domain.Services;

public class LessonService(
    ILessonRepository lessonRepo,
    ILessonPageRepository lessonPageRepo,
    INotebookRepository notebookRepo,
    IUnitOfWork unitOfWork) : ILessonService
{
    public async Task<IReadOnlyList<LessonSummary>> GetByNotebookIdAsync(
        Guid notebookId, Guid userId, CancellationToken ct = default)
    {
        await VerifyNotebookOwnershipAsync(notebookId, userId, ct);
        return await lessonRepo.GetSummariesByNotebookIdAsync(notebookId, ct);
    }

    public async Task<(Lesson Lesson, IReadOnlyList<LessonPage> Pages)> CreateAsync(
        Guid notebookId, Guid userId, string title, CancellationToken ct = default)
    {
        await VerifyNotebookOwnershipAsync(notebookId, userId, ct);

        var now = DateTime.UtcNow;
        var lessonId = Guid.NewGuid();

        var lesson = new Lesson
        {
            Id = lessonId,
            NotebookId = notebookId,
            Title = title,
            CreatedAt = now,
            UpdatedAt = now
        };

        var firstPage = new LessonPage
        {
            Id = Guid.NewGuid(),
            LessonId = lessonId,
            PageNumber = 1,
            ModuleCount = 0
        };

        await lessonRepo.AddAsync(lesson, ct);
        await lessonPageRepo.AddAsync(firstPage, ct);
        await unitOfWork.CommitAsync(ct);

        return (lesson, new List<LessonPage> { firstPage });
    }

    public async Task<(Lesson Lesson, IReadOnlyList<LessonPage> Pages)> GetByIdAsync(
        Guid lessonId, Guid userId, CancellationToken ct = default)
    {
        var result = await lessonRepo.GetWithPagesAsync(lessonId, ct)
                     ?? throw new NotFoundException();

        await VerifyNotebookOwnershipAsync(result.Lesson.NotebookId, userId, ct);
        return result;
    }

    public async Task<(Lesson Lesson, IReadOnlyList<LessonPage> Pages)> UpdateAsync(
        Guid lessonId, Guid userId, string title, CancellationToken ct = default)
    {
        var result = await lessonRepo.GetWithPagesAsync(lessonId, ct)
                     ?? throw new NotFoundException();

        await VerifyNotebookOwnershipAsync(result.Lesson.NotebookId, userId, ct);

        var lesson = result.Lesson;
        lesson.Title = title;
        lesson.UpdatedAt = DateTime.UtcNow;

        lessonRepo.Update(lesson);
        await unitOfWork.CommitAsync(ct);

        return (lesson, result.Pages);
    }

    public async Task DeleteAsync(
        Guid lessonId, Guid userId, CancellationToken ct = default)
    {
        var result = await lessonRepo.GetWithPagesAsync(lessonId, ct)
                     ?? throw new NotFoundException();

        await VerifyNotebookOwnershipAsync(result.Lesson.NotebookId, userId, ct);

        lessonRepo.Remove(result.Lesson);
        await unitOfWork.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<NotebookIndexEntry>> GetNotebookIndexAsync(
        Guid notebookId, Guid userId, CancellationToken ct = default)
    {
        await VerifyNotebookOwnershipAsync(notebookId, userId, ct);

        var summaries = await lessonRepo.GetSummariesByNotebookIdAsync(notebookId, ct);

        var entries = new List<NotebookIndexEntry>(summaries.Count);
        var cumulativePageCount = 0;

        foreach (var summary in summaries)
        {
            entries.Add(new NotebookIndexEntry
            {
                LessonId = summary.Id,
                Title = summary.Title,
                CreatedAt = summary.CreatedAt,
                StartPageNumber = 2 + cumulativePageCount
            });
            cumulativePageCount += summary.PageCount;
        }

        return entries;
    }

    private async Task VerifyNotebookOwnershipAsync(
        Guid notebookId, Guid userId, CancellationToken ct)
    {
        var notebook = await notebookRepo.GetByIdAsync(notebookId, ct)
                       ?? throw new NotFoundException();

        if (notebook.UserId != userId)
            throw new ForbiddenException();
    }
}