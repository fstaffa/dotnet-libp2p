using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace Libp2p.Connection.Multiselect
{
    internal static class VarUInt
    {
        private const byte rest = 127;
        private const byte mostSignificantBit = 128;
        private const byte maxInputs = 9;
        private const byte maxShift = 7 * maxInputs;

        internal static ulong Decode(PipeReader reader)
        {
            ulong result = 0;
            int current;
            int shift = 0;
            do
            {
                current = reader.AsStream().ReadByte();
                if (current == -1)
                {
                    throw new InvalidDataException("Expected another byte for varint");
                }

                var add = (ulong)(((byte)((current) & rest)) << shift);
                result += add;
                shift += 7;
            } while ((current & mostSignificantBit) == 128);

            return result;
        }

        internal static async Task Encode(ulong number, PipeWriter writer)
        {
            var memory = writer.GetMemory(maxInputs);
            int iteration = 0;
            do
            {
                byte current = (byte)(number & rest);
                number = number >> 7;
                if (number > 0)
                {
                    current |= mostSignificantBit;
                }
                memory.Span[iteration] = current;
                iteration++;
            } while (number > 0);
            writer.Advance(iteration);
            await writer.FlushAsync();
        }
    }
}