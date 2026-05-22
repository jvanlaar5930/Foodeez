using System.Threading.Channels;
using Foodeez.Domain.Entities;

namespace Foodeez.Infrastructure.Services;

/// <summary>
/// Unbounded in-memory channel that decouples log writers from the DB writer,
/// so logging never blocks the request pipeline.
/// </summary>
public sealed class LogQueue
{
    private readonly Channel<AppLog> _channel =
        Channel.CreateUnbounded<AppLog>(new UnboundedChannelOptions { SingleReader = true });

    public ChannelWriter<AppLog> Writer => _channel.Writer;
    public ChannelReader<AppLog> Reader => _channel.Reader;
}
