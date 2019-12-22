using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Minotaur.Codecs;
using Minotaur.Core;
using Minotaur.IO;

namespace Minotaur.Streams
{
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
    /// sizeof(T) | First block value.
    /// ----------|-------------------------------
    /// sizeof(T) | Last block value.
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

        private static readonly int sizeOfHeader = sizeof(PayloadHeader) + sizeof(T) * 2;
        private static readonly int sizeOfShell = sizeOfHeader + sizeof(byte);

        private readonly IStream _underlying;
        private readonly ICodec<T> _codec;
        private readonly UnsafeBuffer _decodedBuffer;
        private readonly UnsafeBuffer _encodedBuffer;

        public ColumnStream(IStream underlying, ICodec<T> codec, int capacity = 8192)
        {
            _underlying = underlying;
            _codec = codec;

            _decodedBuffer = new UnsafeBuffer(capacity / sizeof(T) * sizeof(T));
            _encodedBuffer = new UnsafeBuffer(codec.GetMaxEncodedSize(capacity / sizeof(T)) + sizeOfHeader + sizeof(byte));
        }

        #region Implementation of IColumnStream

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
                    if (_underlying.Read(_encodedBuffer.Data, 0, sizeOfHeader) != sizeOfHeader)
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
            _underlying.Reset();
            _decodedBuffer.Reset();
        }

        public void Flush()
        {
            if (_decodedBuffer.Offset > _decodedBuffer.Ptr)
                Write();
            _underlying.Flush();
        }

        #endregion

        public List<BlockInfo<T>> ReadBlockInfos()
        {
            _underlying.Reset();
            var result = new List<BlockInfo<T>>();
            while (true)
            {
                _encodedBuffer.Reset();

                // Read the header
                if (_underlying.Read(_encodedBuffer.Data, 0, sizeOfHeader) != sizeOfHeader)
                    return result; // Ends of stream

                var header = (PayloadHeader*) _encodedBuffer.Ptr;
                var bounds = (T*) (_encodedBuffer.Ptr + sizeof(PayloadHeader));
                
                result.Add(new BlockInfo<T>
                {
                    ShellSize = sizeOfShell,
                    DataLength = header->DataLength,
                    PayloadLength = header->PayloadLength,
                    Version = header->Version,
                    FirstValue = *bounds,
                    LastValue = *(bounds + 1)
                });

                _underlying.Seek(header->PayloadLength + sizeof(byte));
            }
        }

        private void Write()
        {
            _encodedBuffer.Reset();

            var header = (PayloadHeader*)_encodedBuffer.Ptr;
            header->DataLength = (int)(_decodedBuffer.Offset - _decodedBuffer.Ptr) / sizeof(T);
            header->Version = CURRENT_VERSION;

            // Write first and last value of the block
            var bounds = (T*)(_encodedBuffer.Ptr + sizeof(PayloadHeader));
            *(bounds + 1) = *((T*)_decodedBuffer.Ptr + header->DataLength - 1);
            *bounds = *(T*)_decodedBuffer.Ptr;

            header->PayloadLength = _codec.Encode(
                (T*)_decodedBuffer.Ptr,
                header->DataLength,
                _encodedBuffer.Ptr + sizeOfHeader);
            
            * (_encodedBuffer.Ptr + sizeOfHeader + header->PayloadLength) = CHECKSUM;

            _underlying.Write(_encodedBuffer.Data, 0, header->PayloadLength + sizeOfShell);

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