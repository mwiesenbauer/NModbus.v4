using NModbus.Endian;

namespace NModbus.Transport.IP.Mbap;

public static class MbapSerializer

{
    public const ushort PROTOCOL_IDENTIFIER = 0x0000;

    /// <summary>
    /// The length of a MBAP header.
    /// </summary>
    public const ushort MBAP_HEADER_LENGTH = 7;

    public static byte[] SerializeMbapHeader(
        ushort transactionIdentifier,
        ushort length,
        byte unitIdentifier)
    {
        using var writer = new EndianWriter(Endianness.BigEndian);

        writer.Write(transactionIdentifier);
        writer.Write(PROTOCOL_IDENTIFIER);
        writer.Write(length);
        writer.Write(unitIdentifier);

        return writer.ToArray();
    }

    public static MbapHeader DeserializeMbapHeader(
        byte[] buffer)
    {
        if (buffer.Length != MBAP_HEADER_LENGTH)
        {
            throw new InvalidOperationException(
                $"Expected a buffer of size {MBAP_HEADER_LENGTH} but was given a buffer with {buffer.Length} elements.");
        }

        using var reader = new EndianReader(buffer, Endianness.BigEndian);

        return new MbapHeader(
            reader.ReadUInt16(),
            reader.ReadUInt16(),
            reader.ReadUInt16(),
            reader.ReadByte());
    }
}
