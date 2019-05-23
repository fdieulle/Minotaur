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
    /// 4 Bytes   | Block size in number of bytes.
    /// ----------|-------------------------------
    /// 8 Bytes   | Start timestamp. Maybe this one is useless ?
    /// ----------|-------------------------------
    /// 8 Bytes   | End timestamp.
    /// ----------|-------------------------------
    /// Data size | Data
    /// ----------|-------------------------------
    /// 1-4 Bytes | Number of bytes skipped to go at the end of block.
    /// ----------|-------------------------------
    /// Skipped   | Skipped bytes
    ///  Bytes    | 
    /// ----------|-------------------------------
    /// 1 Byte    | Checksum
	/// </summary>
	/// <typeparam name="TStream"></typeparam>
	/// <typeparam name="TCodec"></typeparam>
	public unsafe class ColumnStreamFullStream<TStream, TCodec> : IStream
        where TStream : IStream
        where TCodec : ICodecFullStream
    {
        private const int HEAD_SIZE = sizeof(int);

        private const int SKIP_SIZE = sizeof(int);
        private const int CHECKSUM_SIZE = sizeof(byte);
        private const int TAIL_SIZE = SKIP_SIZE + CHECKSUM_SIZE;

        private const int WRAP_SIZE = HEAD_SIZE + TAIL_SIZE;

        private const byte CHECKSUM = 12;

        private readonly TStream _underlying;
        private readonly TCodec _codec;
        private readonly IAllocator _allocator;

        private readonly byte* _buffer;
        private byte* _blockEnd;
        private byte* _offset;
        private readonly int _capacity;

        public ColumnStreamFullStream(
            TStream underlying,
            TCodec codec,
            IAllocator allocator,
            int capacity = 8192)
        {
            _underlying = underlying;
            _codec = codec;
            _allocator = allocator;
            _capacity = capacity;
            _buffer = allocator.Allocate(capacity);
            _blockEnd = _offset = _buffer;
        }

        public int Read(byte* p, int length)
        {
            var read = 0;
            while (read < length)
            {
                if (_offset >= _blockEnd)
                {
                    // Read tail part if it's not the first block
                    if (_offset != _buffer)
                    {
                        var skip = *(int*)_offset;
                        // Read Checksum
                        if (*(_offset + SKIP_SIZE + skip) != CHECKSUM)
                            throw new CorruptedDataException("Checksum failed");
                    }

                    _blockEnd = _offset = _buffer;
                    if (_underlying.Read(_offset, _capacity) <= 0)
                        return read; // Ends of stream

                    var blockLength = *(int*)_offset;
                    _offset += HEAD_SIZE;
                    _blockEnd = _offset + blockLength;

                    _codec.DecodeHead(ref _offset, blockLength);
                }

                var remainingBytes = (int)(_blockEnd - _offset);
                read += _codec.Decode(ref _offset, remainingBytes, ref p, length - read);

#if Debug
                if((int) (_blockEnd - _offset) <= remainingBytes) 
                    throw new InvalidOperationException($"The codec {typeof(TCodec)} decoded nothing and the read is fallen in an infinity loop");
#endif
            }

            return read;
        }

        /// <summary>
        /// Write columnar data by choosing the best codec to used. i.e. the best compression we can have.
        /// It's better to write a suffisant number of column entries to optimize the compression.
        /// If you write too little bytes you will hurt your performances.
        /// I mean, write data by block is better, but don't worry you can still read entries one by one.
        /// In fact even if you more data than this stream capacity, many blocks of the same capacity will be generated,
        /// untill all data given has been consumed.
        /// </summary>
        /// <param name="p">Data pointer to encode and write.</param>
        /// <param name="length">Length of data to write.</param>
        /// <returns>Number of bytes wrote.</returns>
        public int Write(byte* p, int length)
        {
            var wrote = 0;
            while (wrote < length)
            {
                _offset = _buffer + HEAD_SIZE;
                wrote += _codec.Encode(ref p, length - wrote, ref _offset, _capacity - WRAP_SIZE);

#if Debug
                if(_offset <= _buffer + HEAD_SIZE) 
                    throw new InvalidOperationException($"The codec {typeof(TCodec)} encoded nothing and the write is fallen in an infinity loop");
#endif
                // If the buffer is filled we push data into underlying stream
                if (_offset - _buffer > (_capacity - WRAP_SIZE) * 0.95)
                    Flush();
            }

            return wrote;
        }

        public int Seek(int seek, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public void Reset()
        {
            _blockEnd = _offset = _buffer;
            _underlying.Reset();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Flush()
        {
            var encodedSize = (int)(_offset - _buffer) - HEAD_SIZE;
            if (encodedSize <= 0) return;

            // Write block size before encoded data
            *(int*)_buffer = encodedSize;
            // Write skipped block size just after encoded data
            *(int*)_offset = _capacity - encodedSize - WRAP_SIZE;
            // Write checksum at the end of block
            *(_buffer + (_capacity - CHECKSUM_SIZE)) = CHECKSUM;

            _underlying.Write(_buffer, _capacity);
            _offset = _buffer;
        }

        public void Dispose()
        {
            Flush();
            _allocator.Free(_buffer);
            _underlying.Dispose();
        }
    }
}
