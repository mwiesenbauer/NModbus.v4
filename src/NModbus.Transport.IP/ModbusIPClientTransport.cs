using Microsoft.Extensions.Logging;
using NModbus.Interfaces;

namespace NModbus.Transport.IP;

public class ModbusIPClientTransport : ModbusIPClientTransportBase
{
    private readonly ILogger<ModbusIPClientTransport> _logger;
    private readonly IConnectionStrategy _connectionStrategy;

    public ModbusIPClientTransport(IConnectionStrategy connectionStrategy,
        ILoggerFactory loggerFactory)
    {
        if (loggerFactory is null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        _logger = loggerFactory.CreateLogger<ModbusIPClientTransport>();
        _connectionStrategy = connectionStrategy ?? throw new ArgumentNullException(nameof(connectionStrategy));
    }

    public override async Task<IModbusDataUnit?> SendAndReceiveAsync(IModbusDataUnit message,
        CancellationToken cancellationToken = default)
    {
        await using var streamContainer = await _connectionStrategy.GetStreamContainer(cancellationToken)
            .ConfigureAwait(false);

        var transactionIdentifier = GetNextTransactionIdentifier();

        await streamContainer.Stream.WriteIpMessageAsync(transactionIdentifier, message, cancellationToken);

        var receivedMessage = await streamContainer.Stream.ReadIpMessageAsync(cancellationToken);

        var matches = TransactionIdentifierMatches(receivedMessage, transactionIdentifier);
        return matches switch
        {
            true => receivedMessage,
            _ => throw new InvalidOperationException($"TransactionIdentifier {transactionIdentifier}"),
        };
    }

    private static bool TransactionIdentifierMatches(ModbusIPMessage? msg, ushort transactionIdentifier)
    {
        return msg switch
        {
            null => false,
            _ => msg.Header.TransactionIdentifier == transactionIdentifier
        };
    }

    public override async Task SendAsync(IModbusDataUnit message, CancellationToken cancellationToken = default)
    {
        var transactionIdentifier = GetNextTransactionIdentifier();

        await using var streamContainer = await _connectionStrategy.GetStreamContainer(cancellationToken)
            .ConfigureAwait(false);

        await streamContainer.Stream.WriteIpMessageAsync(transactionIdentifier, message, cancellationToken);
    }

    public override async ValueTask DisposeAsync()
    {
        await _connectionStrategy.DisposeAsync();

        GC.SuppressFinalize(this);
    }
}
