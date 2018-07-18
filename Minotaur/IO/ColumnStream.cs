using System;
using System.IO;
using Minotaur.Codecs;
using Minotaur.Core;

namespace Minotaur.IO
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
	/// </summary>
	/// <typeparam name="TStream"></typeparam>
	/// <typeparam name="TCodec"></typeparam>
	public unsafe class ColumnStream<TStream, TCodec> : IStream
        where TStream : IStream
        where TCodec : ICodec
    {
        private const int HEAD_SIZE = sizeof(int);

        private const int SKIP_SIZE = sizeof(int);
        private const int CHECKSUM_SIZE = sizeof(byte);
        private const int TAIL_SIZE = SKIP_SIZE + CHECKSUM_SIZE;

        private const int WRAP_SIZE = HEAD_SIZE + TAIL_SIZE;

        private const byte CHECKSUM = 12;

        private readonly TStream _underlying;
        private readonly TCodec _codec;

        private readonly byte* _buffer;
        private readonly int _capacity;
        private byte* _blockEnd;
        private byte* _offset;

        public ColumnStream(
            TStream underlying,
            TCodec codec,
            byte* buffer, int length)
        {
            _underlying = underlying;
            _codec = codec;
            _buffer = buffer;
            _capacity = length;
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
                        // Read skipped size
                        var skip = *(int*)_offset;
                        _offset += SKIP_SIZE + skip;

                        // Read Checksum
                        if (*_offset != CHECKSUM)
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

                var rbs = (int)(_blockEnd - _offset); // Remaining buffer size
                var rds = length - read; // Remaining data size
                if (Math.Min(rds, rbs) <= 0) return read;

                read += _codec.Decode(ref _offset, rbs, ref p, rds);
            }

            return read;
        }

        /// <summary>
        /// Write columnar data by choosing the best codec to used. i.e. the best compression we can have.
        /// It's better to write a suffisant number of column entries to optimise the compression.
        /// I mean, write data by block is better, but you can still read entry one by one.
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
                var rds = length - wrote; // Remaining data size
                if (Math.Min(rds, _capacity) <= 0) return wrote;

                _offset += HEAD_SIZE;

                var dataWrote = _codec.Encode(ref p, rds, ref _offset, _capacity - WRAP_SIZE);
                if (dataWrote <= 0)
                {
                    _offset = _buffer;
                    return wrote;
                }

                var encodedSize = (int)(_offset - _buffer) - HEAD_SIZE;
                wrote += dataWrote;

                // Write skip length
                var skip = _capacity - encodedSize - WRAP_SIZE;
                *(int*)_offset = skip;
                _offset += SKIP_SIZE + skip;
                // Write checksum
                *_offset++ = CHECKSUM;

                if (_offset - _buffer != _capacity)
                    throw new CorruptedDataException("Block size corrupted by writer");

                // Write head
                *(int*)_buffer = encodedSize;

                _underlying.Write(_buffer, _capacity);
                _offset = _buffer;
            }

            return wrote;
        }

        public int Seek(int seek, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public void Reset()
        {
            _underlying.Reset();
            _blockEnd = _offset = _buffer;
        }

        public void Flush()
        {
            _underlying.Write(_buffer, (int)(_offset - _buffer));
            _offset = _buffer;
        }

        public void Dispose()
        {
            Flush();
            _underlying.Dispose();
        }
    }
}
