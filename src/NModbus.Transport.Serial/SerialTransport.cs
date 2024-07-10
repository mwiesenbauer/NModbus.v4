using System.Buffers.Binary;
using System.IO.Ports;
using NModbus.Interfaces;
using NModbus.Messages;

namespace NModbus.Transport.Serial;

public class SerialTransport : IModbusClientTransport
{
    private const int RTU_MAX_SIZE = 256;
    private const int RTU_MIN_SIZE = 4;
    private readonly SerialPort _serialPort;
    private readonly int _baudRate;
    private readonly IChecksum _checksum;

    public SerialTransport(SerialPort serialPort)
    {
        _serialPort = serialPort ?? throw new ArgumentNullException(nameof(serialPort));
        _baudRate = serialPort.BaudRate;
        _checksum = new Crc();
    }

    public async Task SendAsync(IModbusDataUnit message, CancellationToken cancellationToken = default)
    {
        var payload = message.ProtocolDataUnit.ToArray();
        var msg = new byte[payload.Length + 3];
        msg[0] = message.UnitIdentifier;
        Array.Copy(payload, 0, msg, 1, payload.Length);

        var crc = BitConverter.GetBytes(_checksum.Calculate(msg.AsSpan()[..^2]));
        Array.Copy(crc, 0, msg, msg.Length - 2, crc.Length);
        await _serialPort.BaseStream.WriteAsync(msg, cancellationToken);

        await Task.Delay(CalculateDelay(msg.Length), cancellationToken);
    }

    public async Task<IModbusDataUnit?> SendAndReceiveAsync(
        IModbusDataUnit message,
        CancellationToken cancellationToken = default
    )
    {
        await SendAsync(message, cancellationToken);
        var bytesToRead = ResponseBytesToRead(message);
        var functionCode = message.ProtocolDataUnit.FunctionCode;
        var functionCodeFail = ModbusFunctionCodes.SetErrorBit(functionCode);

        var frame = new byte[RTU_MAX_SIZE];
        var totalBytesRead = await _serialPort.BaseStream.ReadAsync(frame, 0, RTU_MIN_SIZE, cancellationToken);
        if (totalBytesRead == 0)
        {
            return null;
        }

        if (frame[1] == functionCode)
        {
            while (totalBytesRead < bytesToRead)
            {
                var remainingBytes = bytesToRead - totalBytesRead;
                var bytesRead =
                    await _serialPort.BaseStream.ReadAsync(frame, totalBytesRead, remainingBytes, cancellationToken);
                if (bytesRead == 0)
                {
                    return null;
                }

                totalBytesRead += bytesRead;
            }
        }

        if (frame[1] == functionCodeFail)
        {
            _ = await _serialPort.BaseStream.ReadAsync(frame, totalBytesRead, 5, cancellationToken);
        }

        var msg = frame[..totalBytesRead];
        if (!VerifyCrc(msg))
        {
            return null;
        }

        var data = msg[2..^2];
        return new ModbusDataUnit(
            message.UnitIdentifier,
            new ProtocolDataUnit(functionCode, data)
        );
    }

    private bool VerifyCrc(ReadOnlySpan<byte> msg)
    {
        if (msg.Length < 2)
        {
            return false;
        }

        var expectedResponseCrc = _checksum.Calculate(msg[..^2]);
        var responseCrc = BitConverter.ToUInt16(msg[^2..]);
        return expectedResponseCrc == responseCrc;
    }

    private TimeSpan CalculateDelay(int chars)
    {
        int characterDelay, frameDelay; // us

        if (_baudRate is <= 0 or > 19200)
        {
            characterDelay = 750;
            frameDelay = 1750;
        }
        else
        {
            characterDelay = 15000000 / _baudRate;
            frameDelay = 35000000 / _baudRate;
        }

        return TimeSpan.FromMilliseconds(((characterDelay * chars) + frameDelay) / 1000);
    }

    private static int ResponseBytesToRead(IModbusDataUnit message)
    {
        switch (message.ProtocolDataUnit.FunctionCode)
        {
            case ModbusFunctionCodes.READ_COILS:
            case ModbusFunctionCodes.READ_DISCRETE_INPUTS:
                {
                    var data = message.ProtocolDataUnit.Data.Slice(2, 2).Span;
                    var numberOfRegisters = BinaryPrimitives.ReadUInt16BigEndian(data);
                    return RTU_MIN_SIZE + 1 + ((numberOfRegisters + 7) / 8);
                }
            case ModbusFunctionCodes.READ_HOLDING_REGISTERS:
            case ModbusFunctionCodes.READ_INPUT_REGISTERS:
            case ModbusFunctionCodes.READ_WRITE_MULTIPLE_REGISTERS:
                {
                    var data = message.ProtocolDataUnit.Data.Slice(2, 2).Span;
                    var numberOfRegisters = BinaryPrimitives.ReadUInt16BigEndian(data);
                    return RTU_MIN_SIZE + 1 + (numberOfRegisters * 2);
                }
            case ModbusFunctionCodes.WRITE_SINGLE_COIL:
            case ModbusFunctionCodes.WRITE_MULTIPLE_COILS:
            case ModbusFunctionCodes.WRITE_SINGLE_REGISTER:
            case ModbusFunctionCodes.WRITE_MULTIPLE_REGISTERS:
                {
                    return RTU_MIN_SIZE + 4;
                }
            case ModbusFunctionCodes.MASK_WRITE_REGISTER:
                return RTU_MIN_SIZE + 6;
            default: return RTU_MIN_SIZE;
        }
    }

    public ValueTask DisposeAsync()
    {
        _serialPort.Dispose();
        GC.SuppressFinalize(this);
        return new ValueTask();
    }
}
