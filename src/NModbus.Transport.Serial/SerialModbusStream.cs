using System.IO.Ports;
using NModbus.Interfaces;

namespace NModbus.Transport.Serial;

public sealed class SerialModbusStream : IModbusStream
{
    private const string NEW_LINE = "\r\n";
    private readonly SerialPort _serialPort;

    public SerialModbusStream(SerialPort serialPort)
    {
        serialPort.NewLine = NEW_LINE;
        _serialPort = serialPort;
    }

    public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        return _serialPort.BaseStream.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        return _serialPort.BaseStream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _serialPort.Dispose();
        return default;
    }
}
