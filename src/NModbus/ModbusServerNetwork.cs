using Microsoft.Extensions.Logging;
using NModbus.Interfaces;
using System.Collections.Concurrent;
using NModbus.Messages;

namespace NModbus;

public class ModbusServerNetwork : IModbusServerNetwork
{
    private readonly ConcurrentDictionary<byte, IModbusServer> _servers = new();
    private readonly ILogger _logger;

    public ModbusServerNetwork(ILogger logger)
    {
        _logger = logger;
    }

    public bool TryAddServer(IModbusServer server)
    {
        return _servers.TryAdd(server.UnitIdentifier, server);
    }

    public bool TryRemoveServer(byte unitIdentifier)
    {
        return _servers.TryRemove(unitIdentifier, out _);
    }

    public async Task ProcessRequestAsync(
        IModbusDataUnit requestMessage,
        IModbusClientTransport clientTransport,
        CancellationToken cancellationToken = default)
    {
        if (requestMessage.UnitIdentifier == 0)
        {
            foreach (var server in _servers.Values)
            {
                _ = await server.ProcessRequestAsync(requestMessage.ProtocolDataUnit, cancellationToken);
            }
        }
        else
        {
            if (_servers.TryGetValue(requestMessage.UnitIdentifier, out var server))
            {
                var response = await server.ProcessRequestAsync(requestMessage.ProtocolDataUnit, cancellationToken);
                await clientTransport.SendAsync(new ModbusDataUnit(requestMessage.UnitIdentifier, response),
                    cancellationToken);
            }
            else
            {
                //TODO: Send an exception message that the unit wasn't found. Or do we just timeout. Hmm. Look in the docs.
            }
        }
    }
}
