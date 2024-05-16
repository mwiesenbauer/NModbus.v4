namespace NModbus;

/// <summary>
/// Supported function codes
/// </summary>
public static class ModbusFunctionCodes
{
    /// <summary>
    /// Has the Most Significant Bit Set. Or this with a function code to set the error bit.
    /// </summary>
    public const byte ERROR_MASK = 0b10000000;

    public const byte READ_COILS = 0x01;

    public const byte READ_DISCRETE_INPUTS = 0x02;

    public const byte READ_HOLDING_REGISTERS = 0x03;

    public const byte READ_INPUT_REGISTERS = 0x04;

    public const byte WRITE_SINGLE_COIL = 0x05;

    public const byte WRITE_SINGLE_REGISTER = 0x06;

    public const byte DIAGNOSTICS = 0x08;

    public const byte WRITE_MULTIPLE_COILS = 0x0F;

    public const byte WRITE_MULTIPLE_REGISTERS = 0x10;

    public const byte READ_FILE_RECORD = 0x14;

    public const byte WRITE_FILE_RECORD = 0x15;

    public const byte MASK_WRITE_REGISTER = 0x16;

    public const byte READ_WRITE_MULTIPLE_REGISTERS = 0x17;

    public const byte READ_FIFO_QUEUE = 0x18;

    /// <summary>
    /// Sets the error bit.
    /// </summary>
    /// <param name="functionCode"></param>
    /// <returns></returns>
    public static byte SetErrorBit(byte functionCode)
    {
        return (byte)(functionCode | ERROR_MASK);
    }

    /// <summary>
    /// Removes the error bit.
    /// </summary>
    /// <param name="functionCode"></param>
    /// <returns></returns>
    public static byte RemoveErrorBit(byte functionCode)
    {
        return (byte)(functionCode & 0b01111111);
    }

    /// <summary>
    /// Determines if the error bit is set.
    /// </summary>
    /// <param name="functionCode"></param>
    /// <returns></returns>
    public static bool IsErrorBitSet(byte functionCode)
    {
        return (functionCode & ERROR_MASK) > 0;
    }
}
