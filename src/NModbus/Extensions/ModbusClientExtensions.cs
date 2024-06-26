using NModbus.Interfaces;
using NModbus.Messages;

namespace NModbus.Extensions;

/// <summary>
/// Convenient methods for calling Modbus functions on <see cref="IModbusClient"/>.
/// </summary>
public static class ModbusClientExtensions
{
    /// <summary>
    /// Throws an exception if the specified function isn't available.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="client"></param>
    /// <param name="functionCode"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public static IClientFunction<TRequest, TResponse>? GetClientFunction<TRequest, TResponse>(
        this IModbusClient client,
        byte functionCode)
    {
        var found = client.TryGetClientFunction<TRequest, TResponse>(functionCode, out var clientFunction);
        return found switch
        {
            true => clientFunction,
            _ => throw new KeyNotFoundException(
                $"Unable to find an {nameof(IClientFunction)}<{typeof(TRequest).Name},{typeof(TResponse).Name}> with function code 0x{functionCode:X2}")
        };
    }

    public static async Task<TResponse?> ExecuteAsync<TRequest, TResponse>(
        this IModbusClient client,
        byte functionCode,
        byte unitIdentifier,
        TRequest request,
        CancellationToken cancellationToken = default
    )
    {
        //Find the client function.
        var clientFunction = client.GetClientFunction<TRequest, TResponse>(functionCode);
        if (clientFunction is null)
        {
            throw new InvalidOperationException();
        }

        //Serialize the request
        var serializedRequest = clientFunction.MessageSerializer.SerializeRequest(request);

        //Form the request
        var requestProtocolDataUnit = new ProtocolDataUnit(clientFunction.FunctionCode, serializedRequest);
        var requestMessage = new ModbusDataUnit(unitIdentifier, requestProtocolDataUnit);

        //Check to see if this is a broadcast request.
        if (unitIdentifier == 0)
        {
            //This is a broadcast request. No response is expected
            await client.Transport.SendAsync(requestMessage, cancellationToken);

            return default;
        }

        //Send the request and wait for a response.
        var responseMessage = await client.Transport.SendAndReceiveAsync(requestMessage, cancellationToken);
        if (responseMessage is null)
        {
            return default;
        }

        //Check to see if this is an error response
        if (ModbusFunctionCodes.IsErrorBitSet(responseMessage.ProtocolDataUnit.FunctionCode))
        {
            throw new ModbusServerException((ModbusExceptionCode)responseMessage.ProtocolDataUnit.Data.ToArray()[0]);
        }

        //Deserialize the response.
        return clientFunction.MessageSerializer.DeserializeResponse(responseMessage.ProtocolDataUnit.Data.ToArray());
    }

    public static async Task<bool[]> ReadCoilsAsync(
        this IModbusClient client,
        byte unitIdentifier,
        ushort startingAddress,
        ushort quantityOfOutputs,
        CancellationToken cancellationToken = default)
    {
        var request = new ReadCoilsRequest(startingAddress, quantityOfOutputs);

        var response = await client.ExecuteAsync<ReadCoilsRequest, ReadCoilsResponse>(
            ModbusFunctionCodes.READ_COILS,
            unitIdentifier,
            request,
            cancellationToken);

        return response switch
        {
            null => Array.Empty<bool>(),
            _ => response.Unpack(request.QuantityOfOutputs)
        };
    }

    public static async Task<bool[]> ReadDiscreteInputsAsync(
        this IModbusClient client,
        byte unitIdentifier,
        ushort startingAddress,
        ushort quantityOfInputs,
        CancellationToken cancellationToken = default)
    {
        var request = new ReadDiscreteInputsRequest(startingAddress, quantityOfInputs);

        var response = await client.ExecuteAsync<ReadDiscreteInputsRequest, ReadDiscreteInputsResponse>(
            ModbusFunctionCodes.READ_DISCRETE_INPUTS,
            unitIdentifier,
            request,
            cancellationToken);

        return response switch
        {
            null => Array.Empty<bool>(),
            _ => response.Unpack(request.QuantityOfInputs)
        };
    }

    public static async Task<ushort[]> ReadHoldingRegistersAsync(this IModbusClient client, byte unitIdentifier,
        ushort startingAddress, ushort numberOfRegisters, CancellationToken cancellationToken = default)
    {
        var request = new ReadHoldingRegistersRequest(startingAddress, numberOfRegisters);

        var response = await client.ExecuteAsync<ReadHoldingRegistersRequest, ReadHoldingRegistersResponse>(
            ModbusFunctionCodes.READ_HOLDING_REGISTERS,
            unitIdentifier,
            request,
            cancellationToken);

        return response switch
        {
            null => Array.Empty<ushort>(),
            _ => response.RegisterValues
        };
    }

    public static Task WriteSingleRegisterAsync(this IModbusClient client, byte unitIdentifier,
        ushort startingAddress, ushort value, CancellationToken cancellationToken = default)
    {
        var request = new WriteSingleRegisterRequest(startingAddress, value);

        return client.ExecuteAsync<WriteSingleRegisterRequest, WriteSingleRegisterResponse>(
            ModbusFunctionCodes.WRITE_SINGLE_REGISTER,
            unitIdentifier,
            request,
            cancellationToken
        );
    }

    public static async Task<ushort[]> ReadInputRegistersAsync(this IModbusClient client, byte unitIdentifier,
        ushort startingAddress, ushort numberOfRegisters, CancellationToken cancellationToken = default)
    {
        var request = new ReadInputRegistersRequest(startingAddress, numberOfRegisters);

        var response = await client.ExecuteAsync<ReadInputRegistersRequest, ReadInputRegistersResponse>(
            ModbusFunctionCodes.READ_INPUT_REGISTERS,
            unitIdentifier,
            request,
            cancellationToken);

        return response switch
        {
            null => Array.Empty<ushort>(),
            _ => response.InputRegisters
        };
    }

    public static Task WriteSingleCoilAsync(this IModbusClient client, byte unitIdentifier, ushort outputAddress,
        bool value, CancellationToken cancellationToken = default)
    {
        var request = new WriteSingleCoilRequest(outputAddress, value);

        return client.ExecuteAsync<WriteSingleCoilRequest, WriteSingleCoilResponse>(
            ModbusFunctionCodes.WRITE_SINGLE_COIL,
            unitIdentifier,
            request,
            cancellationToken
        );
    }

    public static Task WriteMultipleCoils(
        this IModbusClient client,
        byte unitIdentifier,
        ushort startingAddress,
        bool[] outputsValue,
        CancellationToken cancellationToken = default)
    {
        var request = new WriteMultipleCoilsRequest(startingAddress, outputsValue);

        return client.ExecuteAsync<WriteMultipleCoilsRequest, WriteMultipleCoilsResponse>(
            ModbusFunctionCodes.WRITE_MULTIPLE_COILS,
            unitIdentifier,
            request,
            cancellationToken
        );
    }

    public static Task WriteMultipleRegistersAsync(this IModbusClient client, byte unitIdentifier,
        ushort startingAddress, ushort[] registers, CancellationToken cancellationToken = default)
    {
        var request = new WriteMultipleRegistersRequest(startingAddress, registers);

        return client.ExecuteAsync<WriteMultipleRegistersRequest, WriteMultipleRegistersResponse>(
            ModbusFunctionCodes.WRITE_MULTIPLE_REGISTERS,
            unitIdentifier,
            request,
            cancellationToken
        );
    }

    //TODO: ReadFileRecord
    //TODO: WriteFileRecord

    public static Task MaskWriteRegisterAsync(
        this IModbusClient client,
        byte unitIdentifier,
        ushort referenceAddress,
        ushort andMask,
        ushort orMask,
        CancellationToken cancellationToken = default)
    {
        var request = new MaskWriteRegisterRequest(referenceAddress, andMask, orMask);

        return client.ExecuteAsync<MaskWriteRegisterRequest, MaskWriteRegisterResponse>(
            ModbusFunctionCodes.MASK_WRITE_REGISTER,
            unitIdentifier,
            request,
            cancellationToken
        );
    }

    public static async Task<ushort[]> ReadWriteMultipleRegisters(
        this IModbusClient client,
        byte unitIdentifier,
        ushort readStartingAddress,
        ushort quantityToRead,
        ushort writeStartingAddress,
        ushort[] writeRegistersValue,
        CancellationToken cancellationToken = default)
    {
        var request = new ReadWriteMultipleRegistersRequest(
            readStartingAddress,
            quantityToRead,
            writeStartingAddress,
            writeRegistersValue);

        var response = await client.ExecuteAsync<ReadWriteMultipleRegistersRequest, ReadWriteMultipleRegistersResponse>(
            ModbusFunctionCodes.READ_WRITE_MULTIPLE_REGISTERS,
            unitIdentifier,
            request,
            cancellationToken);

        return response switch
        {
            null => Array.Empty<ushort>(),
            _ => response.ReadRegistersValue
        };
    }

    public static async Task<ushort[]> ReadFifoQueueAsync(
        this IModbusClient client,
        byte unitIdentifier,
        ushort fifoPointerAddress,
        CancellationToken cancellationToken = default)
    {
        var request = new ReadFifoQueueRequest(fifoPointerAddress);

        var response = await client.ExecuteAsync<ReadFifoQueueRequest, ReadFifoQueueResponse>(
            ModbusFunctionCodes.READ_FIFO_QUEUE,
            unitIdentifier,
            request,
            cancellationToken);

        return response switch
        {
            null => Array.Empty<ushort>(),
            _ => response.FifoValueRegister
        };
    }
}
