using System;
using System.IO;
using System.Runtime.CompilerServices;
using Minotaur.Codecs;
using Minotaur.Core;
using Minotaur.Pocs.Codecs;
using Minotaur.Streams;

namespace Minotaur.Pocs.Streams
{
    /// <summary>
    /// Column stream is responsible to write and read columnar data.
    /// This stream respect a certain block size which is defined by the writer.
    /// When you use this stream as reader, pay attention on the read block size.
    /// It has to be at least equals to the write block size otherwise one of its multiple. 
    ///		For example if you choose a write block size = 8192
    ///		Read block size should be at least 8192 otherwise 16384, 24576, 32768, ...
    /// 
    /// Format details:
    /// 
    ///   Size    | Purpose
    /// ----------|-------------------------------
    /// 4 Bytes   | Block size in number of bytes.
    /// ----------|-------------------------------
    /// Data size | Data
    /// ----------|-------------------------------
    /// 1-4 Bytes | Number of bytes skipped to go at the end of block.
    /// ----------|-------------------------------
    /// Skipped   | Skipped bytes
    ///  Bytes    | 
    /// ----------|-------------------------------
    /// 1 Byte    | Checksum
    ///
    ///
    /// New proposal:
    ///   Size    | Purpose
    /// ----------|-------------------------------
    /// 1 Byte    | Version.
    /// ----------|-------------------------------
    /// 2 Bytes   | Payload length in number of bytes.
    /// ----------|-------------------------------
    /// 2 Bytes   | Uncompressed data length in number of bytes.
    /// ----------|-------------------------------
    /// Data size | Data
    /// ----------|-------------------------------
    /// 1 Byte    | Checksum
    /// </summary>
    /// <typeparam name="TStream"></typeparam>
    /// <typeparam name="TCodec"></typeparam>
    public unsafe class ColumnStream3<TStream, TCodec> : IStream
        where TStream : IStream
        where TCodec : ICodecFullStream
    {
        private const int VERSION_SIZE = sizeof(byte);
        private const int PAYLOAD_LENGTH_SIZE = sizeof(ushort);
        private const int DATA_LENGTH_SIZE = sizeof(ushort);
        private const int HEAD_SIZE = VERSION_SIZE + PAYLOAD_LENGTH_SIZE + DATA_LENGTH_SIZE;

        private const int CHECKSUM_SIZE = sizeof(byte);
        private const int TAIL_SIZE = CHECKSUM_SIZE;

        private const int WRAP_SIZE = HEAD_SIZE + TAIL_SIZE;

        private const byte CURRENT_VERSION = 1;
        private const byte CHECKSUM = 12;

        private readonly TStream _underlying;
        private readonly TCodec _codec;
        private readonly IAllocator _allocator;

        private readonly byte* _buffer;
        private readonly byte* _blockEnd;
        private byte* _offset;
        private readonly int _capacity;

        public ColumnStream3(
            TStream underlying,
            TCodec codec,
            IAllocator allocator,
            int capacity = 8192)
        {
            _underlying = underlying;
            _codec = codec;
            _allocator = allocator;
            _capacity = Math.Max(WRAP_SIZE, Math.Min(capacity, (int)Math.Pow(2, PAYLOAD_LENGTH_SIZE * 8)));
            _buffer = allocator.Allocate(_capacity);
            _offset = _buffer;
            _blockEnd = _buffer + _capacity - TAIL_SIZE;
        }

        #region Implementation of IStream

        public int Read(byte* p, int length)
        {
            throw new System.NotImplementedException();
        }

        public int Write(byte* p, int length)
        {
            var wrote = 0;
            while (wrote < length)
            {
                // Compute wrote step length
                var l = (int)Math.Min(length, _blockEnd - _offset);

                // Copy memory
                MemCopy(ref p, ref _offset, l);

                // Flush to file if ends of buffer is hit
                if(_offset == _blockEnd)
                    Flush();

                wrote += l;
            }

            return wrote;
        }

        public int Seek(int seek, SeekOrigin origin)
        {
            throw new System.NotImplementedException();
        }

        public void Reset()
        {
            throw new System.NotImplementedException();
        }

        public void Flush()
        {
            //var s = _buffer + HEAD_SIZE;
            //_codec.Encode(ref s, _offset - s, )

            //*_buffer = CURRENT_VERSION;
            //*
            //_underlying.
        }

        #endregion

        #region Implementation of IDisposable

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MemCopy(ref byte* src, ref byte* dst, int length)
        {
            while (length >= sizeof(ulong))
            {
                *(ulong*) dst = *(ulong*) src;
                src += sizeof(ulong);
                dst += sizeof(ulong);
                length -= sizeof(ulong);
            }

            while (length > 0)
            {
                *dst++ = *src++;
                length--;
            }
        }
    }
}
