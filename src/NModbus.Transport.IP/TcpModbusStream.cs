using System.Net.Sockets;
using NModbus.Interfaces;

namespace NModbus.Transport.IP;

public class TcpModbusStream : IModbusStream
{
    private readonly TcpClient _tcpClient;
    private readonly Stream _stream;

    public TcpModbusStream(TcpClient tcpClient, Stream stream)
    {
        _tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    }

    public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        return _stream.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        return _stream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _tcpClient.Dispose();
        _stream.Dispose();

        return default;
    }
}
