using System;
using System.IO;
using System.Runtime.CompilerServices;
using Minotaur.Codecs;
using Minotaur.Core;

namespace Minotaur.Streams
{
    /// <inheritdoc />
    /// <summary>
    /// This interface describe a generic stream.
    /// </summary>
    public unsafe interface IColumnStream : IDisposable
    {
        /// <summary>
        /// Read bytes from the stream to th given pointer and for a given length.
        /// </summary>
        /// <param name="p">Pointer to store read bytes.</param>
        /// <param name="length">Number of bytes to read.</param>
        /// <returns>Returns the number of byte read.</returns>
        int Read(byte* p, int length);

        /// <summary>
        /// Write bytes into stream from the given pointer and for a give length.
        /// </summary>
        /// <param name="p">Pointer of data to write.</param>
        /// <param name="length">Number of bytes to write.</param>
        /// <returns>Returns the number of bytes wrote.</returns>
        int Write(byte* p, int length);

        /// <summary>
        /// Reset the stream.
        /// </summary>
        void Reset();

        /// <summary>
        /// Flush buffered data to target output.
        /// </summary>
        void Flush();
    }
    /// <summary>
    /// Column stream is responsible to write and read columnar data.
    /// This stream respect a certain block size which is defined by the writer.
    /// 
    /// Format details:
    /// 
    ///   Size    | Purpose
    /// ----------|-------------------------------
    /// 4 Bytes   | Payload size in bytes.
    /// ----------|-------------------------------
    /// 4 Bytes   | Data count in number of data.
    /// ----------|-------------------------------
    /// 1 Byte    | Version.
    /// ----------|-------------------------------
    /// n Bytes   | Payload data
    /// ----------|-------------------------------
    /// 1 Byte    | Checksum
    /// 
    /// </summary>
    public unsafe class ColumnStream<T> : IColumnStream
        where T : unmanaged
    {
        private const byte CURRENT_VERSION = 1;
        private const byte CHECKSUM = 12;

        private readonly IStream _underlying;
        private readonly ICodec<T> _codec;
        private readonly UnsafeBuffer _decodedBuffer;
        private readonly UnsafeBuffer _encodedBuffer;

        public ColumnStream(IStream underlying, ICodec<T> codec, int capacity = 8192)
        {
            _underlying = underlying;
            _codec = codec;

            _decodedBuffer = new UnsafeBuffer(capacity / sizeof(T) * sizeof(T));
            _encodedBuffer = new UnsafeBuffer(codec.GetMaxEncodedSize(capacity / sizeof(T)) + sizeof(PayloadHeader) + sizeof(byte));
        }

        #region Implementation of IStream

        public int Read(byte* p, int length)
        {
            var read = 0;
            while (read < length)
            {
                // Read the next block from the underlying stream
                if (_decodedBuffer.Offset >= _decodedBuffer.End)
                {
                    _encodedBuffer.Reset();

                    // Read the payload length
                    if (_underlying.Read(_encodedBuffer.Data, 0, sizeof(PayloadHeader)) != sizeof(PayloadHeader))
                        return read; // Ends of stream

                    var payloadLength = ((PayloadHeader*)_encodedBuffer.Ptr)->PayloadLength;
                    if (_underlying.Read(_encodedBuffer.Data, 0, payloadLength + sizeof(byte)) != payloadLength + sizeof(byte))
                        return read; // Ends of stream

                    // Decode data
                    _decodedBuffer.Reset();
                    _decodedBuffer.End += _codec.Decode(_encodedBuffer.Ptr, payloadLength, (T*)_decodedBuffer.Ptr) * sizeof(T);

                    // Checksum coherency
                    if (*(_encodedBuffer.Ptr + payloadLength) != CHECKSUM)
                        throw new CorruptedDataException("Checksum failed");
                }

                var bytesToRead = Math.Min(length - read, (int)(_decodedBuffer.End - _decodedBuffer.Offset));
                Unsafe.CopyBlock(p, _decodedBuffer.Offset, (uint)bytesToRead);
                read += bytesToRead;
                p += bytesToRead;
                _decodedBuffer.Offset += bytesToRead;

#if Debug
                if((int) (_blockEnd - _offset) <= remainingBytes) 
                    throw new InvalidOperationException($"The codec {typeof(TCodec)} decoded nothing and the read is fallen in an infinity loop");
#endif
            }

            return read;
        }

        public int Write(byte* p, int length)
        {
            var wrote = 0;

            do
            {
                var bytesToWrite = Math.Min(
                    _decodedBuffer.Length - (int)(_decodedBuffer.Offset - _decodedBuffer.Ptr), 
                    length - wrote);
                Unsafe.CopyBlock(_decodedBuffer.Offset, p, (uint)bytesToWrite);

                wrote += bytesToWrite;
                _decodedBuffer.Offset += bytesToWrite;
                p += bytesToWrite;

                // Write the next block to the underlying stream
                if (_decodedBuffer.Offset >= _decodedBuffer.Ptr + _decodedBuffer.Length)
                    Write();
            }
            while (wrote < length);

            return wrote;
        }

        public void Reset()
        {
            if (_underlying.CanSeek)
                _underlying.Seek(0, SeekOrigin.Begin);
            _decodedBuffer.Reset();
        }

        public void Flush()
        {
            if (_decodedBuffer.Offset > _decodedBuffer.Ptr)
                Write();
            _underlying.Flush();
        }

        #endregion

        private void Write()
        {
            _encodedBuffer.Reset();

            var header = (PayloadHeader*)_encodedBuffer.Ptr;
            header->DataLength = (int)(_decodedBuffer.Offset - _decodedBuffer.Ptr) / sizeof(T);
            header->Version = CURRENT_VERSION;

            header->PayloadLength = _codec.Encode(
                (T*)_decodedBuffer.Ptr,
                header->DataLength,
                _encodedBuffer.Ptr + sizeof(PayloadHeader));

            *(_encodedBuffer.Ptr + sizeof(PayloadHeader) + header->PayloadLength) = CHECKSUM;

            _underlying.Write(_encodedBuffer.Data, 0, header->PayloadLength + sizeof(PayloadHeader) + sizeof(byte));

            _decodedBuffer.Reset();
        }

        #region Implementation of IDisposable

        public void Dispose()
        {
            _encodedBuffer.Reset();
            _decodedBuffer.Reset();
            _underlying.Dispose();
        }

        #endregion
    }
}