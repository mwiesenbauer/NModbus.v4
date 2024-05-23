using NModbus.Endian;

namespace NModbus.Messages
{
    public class WriteMultipleRegistersMessageSerializer : ModbusMessageSerializer<WriteMultipleRegistersRequest, WriteMultipleRegistersResponse>
    {
        protected override void SerializeRequestCore(WriteMultipleRegistersRequest request, EndianWriter writer)
        {
            writer.Write(request.StartingAddress);
            writer.Write((ushort)(request.Registers.Length));
            writer.Write((byte)(request.Registers.Length * 2));
            writer.Write(request.Registers);
        }

        protected override void SerializeResponseCore(WriteMultipleRegistersResponse response, EndianWriter writer)
        {
            writer.Write(response.StartingAddress);
            writer.Write(response.QuantityOfRegisters);
        }

        protected override WriteMultipleRegistersRequest DeserializeRequestCore(EndianReader reader)
        {
            var startingAddress = reader.ReadUInt16();
            var quantityOfRegisters = reader.ReadUInt16();
            var byteCount = reader.ReadByte();

            //TODO: Reconcile quantityOfRegisters with byteCount

            var registers = reader.ReadUInt16Array(quantityOfRegisters);

            return new WriteMultipleRegistersRequest(startingAddress, registers);
        }

        protected override WriteMultipleRegistersResponse DeserializeResponseCore(EndianReader reader)
        {
            var startingAddress = reader.ReadUInt16();
            var quantityOfRegisters = reader.ReadUInt16();

            return new WriteMultipleRegistersResponse(startingAddress, quantityOfRegisters);
        }
    }

    public record WriteMultipleRegistersRequest(ushort StartingAddress, ushort[] Registers);

    public record WriteMultipleRegistersResponse(ushort StartingAddress, ushort QuantityOfRegisters);
}
