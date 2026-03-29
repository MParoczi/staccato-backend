using Application.BackgroundServices;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using Domain.Services;
using DomainModels.Enums;
using DomainModels.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Tests.Unit;

public class ExportCleanupServiceTests
{
    private readonly Mock<IPdfExportRepository> _exportRepo = new();
    private readonly Mock<IAzureBlobService> _blobService = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private ExportCleanupService CreateService()
    {
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider
            .Setup(p => p.GetService(typeof(IPdfExportRepository)))
            .Returns(_exportRepo.Object);
        serviceProvider
            .Setup(p => p.GetService(typeof(IAzureBlobService)))
            .Returns(_blobService.Object);
        serviceProvider
            .Setup(p => p.GetService(typeof(IUnitOfWork)))
            .Returns(_uow.Object);

        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory
            .Setup(f => f.CreateScope())
            .Returns(scope.Object);

        // IServiceScopeFactory also needs to be resolvable for CreateAsyncScope extension
        serviceProvider
            .Setup(p => p.GetService(typeof(IServiceScopeFactory)))
            .Returns(scopeFactory.Object);

        var logger = NullLoggerFactory.Instance.CreateLogger<ExportCleanupService>();

        return new ExportCleanupService(scopeFactory.Object, logger);
    }

    private static PdfExport MakeExport(
        ExportStatus status,
        string? blobRef = null,
        DateTime? completedAt = null)
    {
        return new PdfExport
        {
            Id = Guid.NewGuid(),
            NotebookId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = status,
            CreatedAt = DateTime.UtcNow.AddHours(-48),
            CompletedAt = completedAt,
            BlobReference = blobRef
        };
    }

    // ── RunCleanupAsync ────────────────────────────────────────────────

    [Fact]
    public async Task RunCleanupAsync_ExpiredReadyWithBlob_DeletesBlobAndRecord()
    {
        var export = MakeExport(ExportStatus.Ready, "exports/blob.pdf",
            DateTime.UtcNow.AddHours(-25));
        _exportRepo
            .Setup(r => r.GetExpiredExportsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PdfExport> { export });

        var service = CreateService();
        await service.RunCleanupAsync(CancellationToken.None);

        _blobService.Verify(
            b => b.DeleteAsync("exports/blob.pdf", It.IsAny<CancellationToken>()), Times.Once);
        _exportRepo.Verify(r => r.Remove(export), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunCleanupAsync_ExpiredFailedWithoutBlob_RemovesRecordOnly()
    {
        var export = MakeExport(ExportStatus.Failed);
        _exportRepo
            .Setup(r => r.GetExpiredExportsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PdfExport> { export });

        var service = CreateService();
        await service.RunCleanupAsync(CancellationToken.None);

        _blobService.Verify(
            b => b.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _exportRepo.Verify(r => r.Remove(export), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunCleanupAsync_NoExpiredExports_DoesNotCommit()
    {
        _exportRepo
            .Setup(r => r.GetExpiredExportsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PdfExport>());

        var service = CreateService();
        await service.RunCleanupAsync(CancellationToken.None);

        _exportRepo.Verify(r => r.Remove(It.IsAny<PdfExport>()), Times.Never);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunCleanupAsync_BlobDeleteFails_ContinuesAndCommits()
    {
        var export1 = MakeExport(ExportStatus.Ready, "exports/fail.pdf",
            DateTime.UtcNow.AddHours(-25));
        var export2 = MakeExport(ExportStatus.Ready, "exports/ok.pdf",
            DateTime.UtcNow.AddHours(-25));

        _exportRepo
            .Setup(r => r.GetExpiredExportsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PdfExport> { export1, export2 });

        _blobService
            .Setup(b => b.DeleteAsync("exports/fail.pdf", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Blob service error"));

        var service = CreateService();
        await service.RunCleanupAsync(CancellationToken.None);

        // Both records should still be removed despite blob failure on first
        _exportRepo.Verify(r => r.Remove(export1), Times.Once);
        _exportRepo.Verify(r => r.Remove(export2), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
