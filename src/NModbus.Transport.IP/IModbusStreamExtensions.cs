using NModbus.Extensions;
using NModbus.Interfaces;
using NModbus.Transport.IP.Mbap;

namespace NModbus.Transport.IP;

public static class ModbusStreamExtensions
{
    public static async Task<ModbusIPMessage?> ReadIpMessageAsync(
        this IModbusStream stream,
        CancellationToken cancellationToken = default)
    {
        var mbapHeaderBuffer = new byte[MbapSerializer.MBAP_HEADER_LENGTH];

        if (!await stream.TryReadBufferAsync(mbapHeaderBuffer, cancellationToken))
        {
            return null;
        }

        var mbapHeader = MbapSerializer.DeserializeMbapHeader(mbapHeaderBuffer);

        var pduBuffer = new byte[mbapHeader.Length - 1];

        var success = await stream.TryReadBufferAsync(pduBuffer, cancellationToken);
        return success switch
        {
            true => new ModbusIPMessage(mbapHeader, new ProtocolDataUnit(pduBuffer)),
            _ => null
        };
    }

    public static async Task WriteIpMessageAsync(
        this IModbusStream stream,
        ushort transactionIdentifier,
        IModbusDataUnit message,
        CancellationToken cancellationToken = default)
    {
        var buffer = message.Serialize(transactionIdentifier);

        //Write it
        await stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
    }
}
