using NModbus.Interfaces;

namespace NModbus;

/// <summary>
/// Simple implementation of <see cref="IModbusStream"/> that wraps a generic <see cref="Stream"/>.
/// </summary>
public class ModbusStream : IModbusStream
{
    private readonly Stream _stream;

    public int ReceiveTimeout
    {
        get => _stream.ReadTimeout;
        set => _stream.ReadTimeout = value;
    }

    public int SendTimeout
    {
        get => _stream.WriteTimeout;
        set => _stream.WriteTimeout = value;
    }

    public ModbusStream(Stream stream)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    }

    public Task<int> ReadAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken = default)
    {
        return _stream.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public Task WriteAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken = default)
    {
        return _stream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _stream.DisposeAsync();
    }
}
