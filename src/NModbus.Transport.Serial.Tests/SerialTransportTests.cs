using System.IO.Ports;
using Moq;
using NModbus.Interfaces;
using NModbus.Messages;

namespace NModbus.Transport.Serial.Tests;

public class SerialTransportTests
{
    [Fact]
    public void StreamIsNull()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                var transport = new SerialTransport(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            }
        );
    }

    [Fact]
    public async Task ReadHoldingRegister()
    {
        var expected = new byte[8] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x01, 0x84, 0xA };

        var stream = new MemoryStream();
        var serialPort = new FixedStreamSerialPort(stream);
        var transport = new SerialTransport(serialPort);
        var request = new ReadHoldingRegistersRequest(0, 1);
        var serializer = new ReadHoldingRegistersMessageSerializer();
        var requestPayload = serializer.SerializeRequest(request);
        var protocolDataUnit = new ProtocolDataUnit(ModbusFunctionCodes.READ_HOLDING_REGISTERS, requestPayload);
        var msg = new ModbusDataUnit(1, protocolDataUnit);

        await transport.SendAsync(msg);
        Assert.Equal(expected, stream.ToArray());
    }

    [Fact]
    public async Task WriteHoldingRegister()
    {
        var expectedResponse = new byte[] { 0x01, 0x06, 0x0, 0x4, 0x0, 0x1, 0x9, 0xcb };
        Assert.NotNull(expectedResponse);

        var stream = new Mock<Stream>();
        var sequence = new MockSequence();
        _ = stream
            .InSequence(sequence)
            .Setup(
                s => s.ReadAsync(
                    It.IsAny<Memory<byte>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback((Memory<byte> buffer, CancellationToken _) => { expectedResponse.AsMemory(..4).CopyTo(buffer); })
            .ReturnsAsync(4);
        _ = stream
            .InSequence(sequence)
            .Setup(
                s => s.ReadAsync(
                    It.IsAny<Memory<byte>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback((Memory<byte> buffer, CancellationToken _) => { expectedResponse.AsMemory(4..).CopyTo(buffer); })
            .ReturnsAsync(4);

        var serialPort = new FixedStreamSerialPort(stream.Object);
        var transport = new SerialTransport(serialPort);
        var request = new WriteSingleRegisterRequest(0, 1);
        var serializer = new WriteSingleRegisterMessageSerializer();
        var requestPayload = serializer.SerializeRequest(request);
        var protocolDataUnit = new ProtocolDataUnit(ModbusFunctionCodes.WRITE_SINGLE_REGISTER, requestPayload);
        var msg = new ModbusDataUnit(1, protocolDataUnit);

        var response = await transport.SendAndReceiveAsync(msg);
        Assert.NotNull(response);
        Assert.Equal(1, response.UnitIdentifier);
        Assert.NotNull(response.ProtocolDataUnit);
        Assert.Equal(ModbusFunctionCodes.WRITE_SINGLE_REGISTER, response.ProtocolDataUnit.FunctionCode);
        Assert.Equal(expectedResponse.AsMemory(2, 4), response.ProtocolDataUnit.Data);
    }

    [Fact]
    public void CalculateCrcNullArray()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            new Crc().Calculate(null)
        );
    }

    [Fact]
    public void CalculateCrcZeroArray()
    {
        var crc = new Crc().Calculate(Array.Empty<byte>());
        Assert.Equal(ushort.MaxValue, crc);
    }

    [Fact]
    public void CalculateCrc()
    {
        var crc = new Crc().Calculate(new byte[] { 0x02, 0x07 });
        Assert.Equal(0x1241, crc);
    }
}

internal class FixedStreamSerialPort : SerialPort
{
#pragma warning disable IDE0032
    private readonly Stream _stream;

    public FixedStreamSerialPort(Stream stream)
    {
        _stream = stream;
    }

    public new Stream BaseStream => _stream;
#pragma warning restore IDE0032
}
