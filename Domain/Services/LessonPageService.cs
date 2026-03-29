using Domain.Exceptions;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using DomainModels.Models;

namespace Domain.Services;

public class LessonPageService(
    ILessonRepository lessonRepo,
    ILessonPageRepository lessonPageRepo,
    INotebookRepository notebookRepo,
    IUnitOfWork unitOfWork) : ILessonPageService
{
    public async Task<IReadOnlyList<LessonPage>> GetByLessonIdAsync(
        Guid lessonId, Guid userId, CancellationToken ct = default)
    {
        await VerifyLessonOwnershipAsync(lessonId, userId, ct);
        return await lessonPageRepo.GetByLessonIdOrderedAsync(lessonId, ct);
    }

    public async Task<(LessonPage Page, bool IsOverSoftLimit)> AddPageAsync(
        Guid lessonId, Guid userId, CancellationToken ct = default)
    {
        await VerifyLessonOwnershipAsync(lessonId, userId, ct);

        var maxPageNumber = await lessonPageRepo.GetMaxPageNumberByLessonIdAsync(lessonId, ct);
        var pageCount = await lessonPageRepo.GetPageCountByLessonIdAsync(lessonId, ct);

        var page = new LessonPage
        {
            Id = Guid.NewGuid(),
            LessonId = lessonId,
            PageNumber = maxPageNumber + 1,
            ModuleCount = 0
        };

        await lessonPageRepo.AddAsync(page, ct);
        await unitOfWork.CommitAsync(ct);

        return (page, pageCount >= 10);
    }

    public async Task DeletePageAsync(
        Guid lessonId, Guid pageId, Guid userId, CancellationToken ct = default)
    {
        await VerifyLessonOwnershipAsync(lessonId, userId, ct);

        var page = await lessonPageRepo.GetByIdAsync(pageId, ct)
                   ?? throw new NotFoundException();

        if (page.LessonId != lessonId)
            throw new NotFoundException();

        var pageCount = await lessonPageRepo.GetPageCountByLessonIdAsync(lessonId, ct);
        if (pageCount < 2)
            throw new BadRequestException("LAST_PAGE_DELETION",
                "Cannot delete the last remaining page of a lesson.");

        lessonPageRepo.Remove(page);
        await unitOfWork.CommitAsync(ct);
    }

    private async Task VerifyLessonOwnershipAsync(
        Guid lessonId, Guid userId, CancellationToken ct)
    {
        var lesson = await lessonRepo.GetByIdAsync(lessonId, ct)
                     ?? throw new NotFoundException();

        var notebook = await notebookRepo.GetByIdAsync(lesson.NotebookId, ct)
                       ?? throw new NotFoundException();

        if (notebook.UserId != userId)
            throw new ForbiddenException();
    }
}
