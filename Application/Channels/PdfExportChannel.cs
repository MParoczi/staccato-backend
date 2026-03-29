using System.Threading.Channels;
using Domain.Interfaces;

namespace Application.Channels;

public sealed class PdfExportChannel : IPdfExportQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateBounded<Guid>(
        new BoundedChannelOptions(50)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

    public ChannelReader<Guid> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(Guid exportId, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(exportId, ct);
}
