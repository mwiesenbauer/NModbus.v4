namespace NModbus.Transport.Serial;

public interface IChecksum
{
    ushort Calculate(ReadOnlySpan<byte> data);
}
