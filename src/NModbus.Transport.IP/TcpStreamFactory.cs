using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using NModbus.Interfaces;

namespace NModbus.Transport.IP;

/// <summary>
/// Responsible for creating a client.
/// </summary>
public class TcpStreamFactory : IStreamFactory
{
    private readonly IPEndPoint _endPoint;
    private readonly Action<TcpClient> _tcpClientConfig;
    private readonly SslClientAuthenticationOptions? _sslOptions;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="endPoint"></param>
    /// <param name="tcpClientConfig">Custom configuration action for <see cref="TcpClient"/>.</param>
    /// <param name="sslOptions">If non-null, an ssl connection will be created with the specified options.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public TcpStreamFactory(
        IPEndPoint endPoint,
        Action<TcpClient>? tcpClientConfig = null,
        SslClientAuthenticationOptions? sslOptions = null
    )
    {
        _endPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
        _tcpClientConfig = tcpClientConfig ?? (_ => { });
        _sslOptions = sslOptions;
    }

    /// <summary>
    /// Creates a connection and performs a connection.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <remarks>
    /// Optionally creates an <see cref="SslStream"/> if an <see cref="SslClientAuthenticationOptions"/> instance is provided in the constructor.
    /// </remarks>
    public async Task<IModbusStream> CreateAndConnectAsync(CancellationToken cancellationToken)
    {
        var tcpClient = new TcpClient();

        _tcpClientConfig(tcpClient);

        await tcpClient.ConnectAsync(_endPoint.Address, _endPoint.Port);

        if (_sslOptions == null)
        {
            return new TcpModbusStream(tcpClient, tcpClient.GetStream());
        }

        var sslStream = new SslStream(
            tcpClient.GetStream(),
            false);

        await sslStream.AuthenticateAsClientAsync(_sslOptions, cancellationToken);

        return new TcpModbusStream(tcpClient, sslStream);
    }
}
