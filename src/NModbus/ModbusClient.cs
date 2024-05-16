using Microsoft.Extensions.Logging;
using NModbus.Extensions;
using NModbus.Functions;
using NModbus.Interfaces;
using NModbus.Messages;

namespace NModbus;

public class ModbusClient : IModbusClient
{
    private readonly Dictionary<byte, IClientFunction> _clientFunctions;
    private readonly ILogger _logger;

    public ModbusClient(
        IModbusClientTransport transport,
        ILoggerFactory loggerFactory,
        IEnumerable<IClientFunction>? customClientFunctions = null
    )
    {
        if (loggerFactory is null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        Transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _logger = loggerFactory.CreateLogger<ModbusClient>();

        var defaultClientFunctions = new IClientFunction[]
        {
            new ModbusClientFunction<ReadCoilsRequest, ReadCoilsResponse>(ModbusFunctionCodes.READ_COILS,
                new ReadCoilsMessageSerializer()),
            new ModbusClientFunction<ReadDiscreteInputsRequest, ReadDiscreteInputsResponse>(
                ModbusFunctionCodes.READ_DISCRETE_INPUTS, new ReadDiscreteInputsMessageSerializer()),
            new ModbusClientFunction<ReadHoldingRegistersRequest, ReadHoldingRegistersResponse>(
                ModbusFunctionCodes.READ_HOLDING_REGISTERS, new ReadHoldingRegistersMessageSerializer()),
            new ModbusClientFunction<ReadInputRegistersRequest, ReadInputRegistersResponse>(
                ModbusFunctionCodes.READ_INPUT_REGISTERS, new ReadInputRegistersMessageSerializer()),
            new ModbusClientFunction<WriteSingleCoilRequest, WriteSingleCoilResponse>(
                ModbusFunctionCodes.WRITE_SINGLE_COIL, new WriteSingleCoilMessageSerializer()),
            new ModbusClientFunction<WriteSingleRegisterRequest, WriteSingleRegisterResponse>(
                ModbusFunctionCodes.WRITE_SINGLE_REGISTER, new WriteSingleRegisterMessageSerializer()),
            new ModbusClientFunction<WriteMultipleCoilsRequest, WriteMultipleCoilsResponse>(
                ModbusFunctionCodes.WRITE_MULTIPLE_COILS, new WriteMultipleCoilsMessageSerializer()),
            new ModbusClientFunction<WriteMultipleRegistersRequest, WriteMultipleRegistersResponse>(
                ModbusFunctionCodes.WRITE_MULTIPLE_REGISTERS, new WriteMultipleRegistersMessageSerializer()),
            new ModbusClientFunction<ReadFileRecordRequest, ReadFileRecordResponse>(
                ModbusFunctionCodes.READ_FILE_RECORD, new ReadFileRecordMessageSerializer()),
            new ModbusClientFunction<WriteFileRecordRequest, WriteFileRecordResponse>(
                ModbusFunctionCodes.WRITE_FILE_RECORD, new WriteFileRecordMessageSerializer()),
            new ModbusClientFunction<MaskWriteRegisterRequest, MaskWriteRegisterResponse>(
                ModbusFunctionCodes.MASK_WRITE_REGISTER, new MaskWriteRegisterMessageSerializer()),
            new ModbusClientFunction<ReadWriteMultipleRegistersRequest, ReadWriteMultipleRegistersResponse>(
                ModbusFunctionCodes.READ_WRITE_MULTIPLE_REGISTERS,
                new ReadWriteMultipleRegistersMessageSerializer()),
            new ModbusClientFunction<ReadFifoQueueRequest, ReadFifoQueueResponse>(
                ModbusFunctionCodes.READ_FIFO_QUEUE, new ReadFifoQueueMessageSerializer()),
        };

        _clientFunctions = defaultClientFunctions
            .ToDictionary(f => f.FunctionCode);

        if (customClientFunctions == null)
        {
            return;
        }

        //Now allow the caller to override any of the client functions (or add new ones).
        foreach (var clientFunction in customClientFunctions)
        {
            _logger.LogInformation("Custom implementation of function code {FunctionCode} with type {Type}.",
                $"0x{clientFunction.FunctionCode}", clientFunction.GetType().Name);
            _clientFunctions[clientFunction.FunctionCode] = clientFunction;
        }
    }

    public IModbusClientTransport Transport { get; }

    public virtual bool TryGetClientFunction<TRequest, TResponse>(byte functionCode,
        out IClientFunction<TRequest, TResponse>? clientFunction)
    {
        clientFunction = null;

        if (!_clientFunctions.TryGetValue(functionCode, out var baseClientFunction))
        {
            _logger.LogWarning("Unable to find client function with function code {FunctionCode}",
                functionCode.ToHex());
            return false;
        }

        clientFunction = baseClientFunction as IClientFunction<TRequest, TResponse>;

        if (clientFunction == null)
        {
            _logger.LogWarning(
                "A client function was found for function code {FunctionCode} but it was not of type " +
                nameof(IClientFunction) + "<{TRequest},{TResponse}>", functionCode, typeof(TRequest).Name,
                typeof(TResponse).Name);
        }

        return clientFunction != null;
    }
}
