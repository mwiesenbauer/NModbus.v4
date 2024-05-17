using Microsoft.Extensions.Logging;
using NModbus.Interfaces;

namespace NModbus.Transport.IP.ConnectionStrategies;

public class SingletonStreamConnectionStrategy : IConnectionStrategy
{
    private readonly IStreamFactory _tcpClientFactory;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private IModbusStream? _stream;

    public SingletonStreamConnectionStrategy(IStreamFactory streamFactory, ILoggerFactory loggerFactory)
    {
        if (loggerFactory == null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        _tcpClientFactory = streamFactory ?? throw new ArgumentNullException(nameof(streamFactory));
    }

    public async Task<IPerRequestStreamContainer> GetStreamContainer(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        _stream ??= await _tcpClientFactory.CreateAndConnectAsync(cancellationToken);
        _ = _semaphore.Release();

        return new SingletonStreamPerRequestContainer(_stream);
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        if (_stream != null)
        {
            await _stream.DisposeAsync();
        }
    }
}
