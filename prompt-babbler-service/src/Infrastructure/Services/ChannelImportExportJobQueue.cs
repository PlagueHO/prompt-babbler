using System.Threading.Channels;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Infrastructure.Services;

public sealed class ChannelImportExportJobQueue : IImportExportJobQueue
{
    private readonly Channel<ImportExportJobQueueItem> _channel;

    public ChannelImportExportJobQueue()
    {
        _channel = Channel.CreateBounded<ImportExportJobQueueItem>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        });
    }

    public async ValueTask EnqueueAsync(ImportExportJobQueueItem queueItem, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(queueItem, cancellationToken);
    }

    public async ValueTask<ImportExportJobQueueItem> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return await _channel.Reader.ReadAsync(cancellationToken);
    }
}
