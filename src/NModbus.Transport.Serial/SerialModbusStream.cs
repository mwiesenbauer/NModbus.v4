using System.IO.Ports;
using NModbus.Interfaces;

namespace NModbus.Transport.Serial;

public class SerialModbusStream : IModbusStream
{
    private const string NEW_LINE = "\r\n";
    private readonly SerialPort _serialPort;

    public SerialModbusStream(SerialPort serialPort)
    {
        _serialPort = serialPort;
        _serialPort.NewLine = NEW_LINE;
    }

    public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        var read = _serialPort.Read(buffer, offset, count);

        return Task.FromResult(read);
    }

    public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        _serialPort.Write(buffer, offset, count);

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _serialPort.Dispose();
        GC.SuppressFinalize(this);
        return default;
    }
}
