namespace NModbus.Transport.IP;

/// <summary>
/// Default ports for Modbus over TCP.
/// </summary>
public static class ModbusIPPorts
{
    /// <summary>
    /// 502: mbap/TCP
    /// </summary>
    public const int INSECURE = 502;

    /// <summary>
    /// 802: mbap/TLS/TCP
    /// </summary>
    public const int SECURE = 802;
}
