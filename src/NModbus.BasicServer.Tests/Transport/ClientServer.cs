using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using NModbus.Interfaces;
using NModbus.Transport.IP;
using NModbus.Transport.IP.ConnectionStrategies;

namespace NModbus.BasicServer.Tests.Transport;

public class ClientServer : IAsyncDisposable
{
    private const int PORT = 5502;
    private readonly ModbusTcpServerNetworkTransport _serverTransport;
    private readonly IModbusClientTransport _clientTransport;

    public ClientServer(byte unitIdentifier, ILoggerFactory loggerFactory)
    {
        if (loggerFactory is null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        UnitIdentifier = unitIdentifier;

        //Create the server
        var serverNetwork = new ModbusServerNetwork(loggerFactory.CreateLogger<ModbusServerNetwork>());

        var serverFunctions = ServerFunctionFactory.CreateBasicServerFunctions(Storage, loggerFactory);

        var server = new ModbusServer(UnitIdentifier, serverFunctions, loggerFactory);

        if (!serverNetwork.TryAddServer(server))
        {
            throw new InvalidOperationException($"Unable to add server with unit number {server.UnitIdentifier}");
        }

        var tcpListener = new TcpListener(IPAddress.Loopback, PORT);

        _serverTransport = new ModbusTcpServerNetworkTransport(tcpListener, serverNetwork, loggerFactory);

        var tcpClientFactory = new TcpStreamFactory(new IPEndPoint(IPAddress.Loopback, PORT));

        //Create the client
        var tcpClientLifetime = new SingletonStreamConnectionStrategy(tcpClientFactory, loggerFactory);
        _clientTransport = new ModbusIPClientTransport(tcpClientLifetime, loggerFactory);
        Client = new ModbusClient(_clientTransport, loggerFactory);
    }

    public byte UnitIdentifier { get; }

    public IModbusClient Client { get; }

    public Storage Storage { get; } = new();

    public async ValueTask DisposeAsync()
    {
        await _serverTransport.DisposeAsync();
        await _clientTransport.DisposeAsync();

        GC.SuppressFinalize(this);
    }
}
