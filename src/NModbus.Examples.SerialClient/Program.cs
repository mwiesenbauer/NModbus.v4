using System.IO.Ports;
using Microsoft.Extensions.Logging;
using NModbus;
using NModbus.Extensions;
using NModbus.Transport.Serial;

const int baudRate = 115200;
const int dataBits = 8;
var loggerFactory = LoggerFactory.Create(builder =>
{
    _ = builder
        .SetMinimumLevel(LogLevel.Debug)
        .AddConsole();
});

var timeout = (int)TimeSpan.FromMilliseconds(100).TotalMilliseconds;
var serialPort = new SerialPort("/dev/tty.usbserial-FT4YFJ4T3", baudRate, Parity.None, dataBits, StopBits.Two)
{
    NewLine = "\r\n",
    ReadTimeout = timeout,
    WriteTimeout = timeout
};
serialPort.Open();
serialPort.BaseStream.ReadTimeout = timeout;
serialPort.BaseStream.WriteTimeout = timeout;

var serialTransport = new SerialTransport(serialPort);
var client = new ModbusClient(serialTransport, loggerFactory);
using var cts = new CancellationTokenSource();
await client.WriteSingleRegisterAsync(0, 4, 1, cts.Token);
await Task.Delay(TimeSpan.FromMilliseconds(50), cts.Token);

for (byte address = 1; address < byte.MaxValue; address++)
{
    // await client.WriteSingleRegisterAsync(address, 4, 1, cts.Token);

    var registers = await client.ReadHoldingRegistersAsync(address, 0, 1, cts.Token);
    if (registers.Length > 0)
    {
        Console.WriteLine(registers[0]);
    }
}
