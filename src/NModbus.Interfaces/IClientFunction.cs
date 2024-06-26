namespace NModbus.Interfaces;

public interface IClientFunction
{
    byte FunctionCode { get; }
}

public interface IClientFunction<TRequest, TResponse> : IClientFunction
{
    IModbusMessageSerializer<TRequest, TResponse> MessageSerializer { get; }
}
