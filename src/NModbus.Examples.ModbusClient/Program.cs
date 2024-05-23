using System.Net;
using Microsoft.Extensions.Logging;
using NModbus;
using NModbus.Examples.ModbusClient;
using NModbus.Extensions;

var loggerFactory = LoggerFactory.Create(builder =>
{
    _ = builder
        .SetMinimumLevel(LogLevel.Debug)
        .AddConsole();
});

var logger = loggerFactory.CreateLogger<Program>();

//The unit number of the modbus server
const byte unitIdentifier = 1;

var sampleFactory = new ModbusIpClientSampleTransportFactory(loggerFactory);

var sample = "insecure";

await using var transport = sample switch
{
    // create a "standard" modbus tcp client
    "insecure" => sampleFactory.CreateTcpInsecureClient(IPAddress.Loopback),

    // create a modbus secure client, accepting all certificates
    "secure" => await sampleFactory.CreateTcpSecureClient("localhost", (snd, cert, chain, errors) => true),

    // create a "standard" modbus upd client
    "udp" => sampleFactory.CreateUpdClient(IPAddress.Loopback),

    _ => throw new NotSupportedException("Only 'insecure', 'secure' or 'udp' is supported as option")
};

var modbusClient = new ModbusClient(transport, loggerFactory);

logger.LogInformation("Writing a single register...");

await modbusClient.WriteSingleRegisterAsync(unitIdentifier, 0, 44);
{
    var holdingRegisters = await modbusClient.ReadHoldingRegistersAsync(unitIdentifier, 0, 5);

    logger.LogInformation("Read Holding Registers: {Registers}",
        string.Join(", ", holdingRegisters.Select(r => r.ToString())));
}

logger.LogInformation("Write multiple registers..");

await modbusClient.WriteMultipleRegistersAsync(unitIdentifier, 0, new ushort[] { 42, 43, 44 });

{
    var holdingRegisters = await modbusClient.ReadHoldingRegistersAsync(unitIdentifier, 0, 5);

    logger.LogInformation("Read Holding Registers: {Registers}",
        string.Join(", ", holdingRegisters.Select(r => r.ToString())));
}

Console.WriteLine("Press any key to exit...");
Console.ReadKey();
