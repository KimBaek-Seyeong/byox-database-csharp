using System;
using System.ComponentModel;

namespace Core
{
    /*
     * Helper class contains static method that read and write numeric values
     * into and from a byte array in little endian byte order.
     */
    public static class BufferHelper
    {
        /*
         * MSB : Most Significant Bit/Byte. The MSB has the greatest influence on the overall value of the number.
         * LSB : Least Significant Bit/Byte. The LSB has the least influence on the overall value of the number.
         *
         * Big-endian and little-endian refer to the order in which bytes are arranged in computer memory for multi-byte data types.
         *
         * In big-endian systems, the MSB is stored at the lowest memory address. (left -> right; 12 34 56 78)
         * => network protocolos often use it
         * ==> (GoodSide) Easier for humans to understand, great for consistent communication between different systems.
         * ==> (BadSide) a bit slower, and figuring out the data layout is not as straightforward.
         *
         * In little-endian systems, the LSB is stored at the lowest memory address. (right -> left; 78 56 34 12)
         * => most modern computer architectures, x86 and ARM, use it
         * ==> (GoodSide) Efficient for computers, easy to figure out where data starts.
         * ==> (BadSide) need to know about it for working code that works on diffrent computers.
         */
        private static readonly bool IsLittleEndian = BitConverter.IsLittleEndian;

        #region Read Buffer
        // ReadBuffer
        public static Guid ReadBufferGuid(byte[] buffer, int bufferOffset)
        {
            /*
             * In the .NET Framework, Globally Unique Identifier(Guid) is designed to be endian-independent.
             * Guid is consist of many parts, some of the part is stored at big-endian, and others are stored at little-endian.
             * The .NET Framework already guarantees the correct byte order when generating and processing Guids.
             * Therefore, Guids don't need to use "ReverseBytes", as the .NET Framework's Guid implementation already handles all endian-related issues.
             */
            return new Guid(new ReadOnlySpan<byte>(buffer, bufferOffset, 16));
        }

        public static uint ReadBufferUInt32(byte[] buffer, int bufferOffset)
        {
            uint value = BitConverter.ToUInt32(buffer, bufferOffset);
            return IsLittleEndian ? value : ReverseBytes(value);
        }

        public static int ReadBufferInt32(byte[] buffer, int bufferOffset)
        {
            int value = BitConverter.ToInt32(buffer, bufferOffset);
            return IsLittleEndian ? value : ReverseBytes(value);
        }

        public static long ReadBufferInt64(byte[] buffer, int bufferOffset)
        {
            long value = BitConverter.ToInt64(buffer, bufferOffset);
            return IsLittleEndian ? value : ReverseBytes(value);
        }

        public static double ReadBufferDouble(byte[] buffer, int bufferOffset)
        {
            if (IsLittleEndian)
            {
                return BitConverter.ToDouble(buffer, bufferOffset);
            }
            else
            {
                var reversedBuffer = new byte[8];
                Array.Copy(buffer, bufferOffset, reversedBuffer, 0, 8);
                Array.Reverse(reversedBuffer);
                return BitConverter.ToDouble(reversedBuffer, 0);
            }
        }
        #endregion

        #region Write Buffer
        // WriteBuffer
        public static void WriteBuffer(Guid value, byte[] buffer, int bufferOffset)
        {
            Buffer.BlockCopy(value.ToByteArray(), 0, buffer, bufferOffset, 16);
        }

        public static void WriteBuffer(uint value, byte[] buffer, int bufferOffset)
        {
            if (IsLittleEndian)
            {
                BitConverter.TryWriteBytes(new Span<byte>(buffer, bufferOffset, 4), value);
            }
            else
            {
                var bytes = BitConverter.GetBytes(value);
                Array.Reverse(bytes);
                Buffer.BlockCopy(bytes, 0, buffer, bufferOffset, 4);
            }
        }

        public static void WriteBuffer(int value, byte[] buffer, int bufferOffset)
        {
            if (IsLittleEndian)
            {
                BitConverter.TryWriteBytes(new Span<byte>(buffer, bufferOffset, 4), value);
            }
            else
            {
                var bytes = BitConverter.GetBytes(value);
                Array.Reverse(bytes);
                Buffer.BlockCopy(bytes, 0, buffer, bufferOffset, 4);
            }
        }

        public static void WriteBuffer(long value, byte[] buffer, int bufferOffset)
        {
            if (IsLittleEndian)
            {
                BitConverter.TryWriteBytes(new Span<byte>(buffer, bufferOffset, 8), value);
            }
            else
            {
                var bytes = BitConverter.GetBytes(value);
                Array.Reverse(bytes);
                Buffer.BlockCopy(bytes, 0, buffer, bufferOffset, 8);
            }
        }

        public static void WriteBuffer(double value, byte[] buffer, int bufferOffset)
        {
            if (IsLittleEndian)
            {
                BitConverter.TryWriteBytes(new Span<byte>(buffer, bufferOffset, 8), value);
            }
            else
            {
                var bytes = BitConverter.GetBytes(value);
                Array.Reverse(bytes);
                Buffer.BlockCopy(bytes, 0, buffer, bufferOffset, 8);
            }
        }
        #endregion

        #region Reverse Bytes
        // Reverse Bytes
        private static uint ReverseBytes(uint value)
        {
            return (value & 0x000000FFU) << 24
                | (value & 0x0000FF00U) << 8
                | (value & 0x00FF0000U) >> 8
                | (value & 0xFF000000U) >> 24;
        }

        private static int ReverseBytes(int value)
        {
            return (int)ReverseBytes((uint)value);
        }

        private static long ReverseBytes(long value)
        {
            return (long)ReverseBytes((ulong)value);
        }

        private static ulong ReverseBytes(ulong value)
        {
            return (value & 0x00000000000000FFUL) << 56
                | (value & 0x000000000000FF00UL) << 40
                | (value & 0x0000000000FF0000UL) << 24
                | (value & 0x00000000FF000000UL) << 8
                | (value & 0x000000FF00000000UL) >> 8
                | (value & 0x0000FF0000000000UL) >> 24
                | (value & 0x00FF000000000000UL) >> 40
                | (value & 0xFF00000000000000UL) >> 56;
        }
        #endregion
    }
}
