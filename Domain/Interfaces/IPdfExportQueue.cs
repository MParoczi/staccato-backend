namespace Domain.Interfaces;

public interface IPdfExportQueue
{
    ValueTask EnqueueAsync(Guid exportId, CancellationToken ct = default);
}
