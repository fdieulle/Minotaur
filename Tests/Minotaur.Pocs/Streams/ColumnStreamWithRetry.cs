using System;
using System.IO;
using System.Runtime.CompilerServices;
using Minotaur.Codecs;
using Minotaur.Core;
using Minotaur.Streams;

namespace Minotaur.Pocs.Streams
{
    /// <summary>
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
    public unsafe class ColumnStreamWithRetry<T, TCodec> : IColumnStream
        where T : unmanaged
        where TCodec : ICodec<T>
    {
        private const int CURRENT_VERSION = 1;
        private const byte CHECKSUM = 12;

        private readonly Stream _underlying;
        private readonly TCodec _codec;
        private readonly UnsafeBuffer _dataBuffer;
        private readonly UnsafeBuffer _payloadBuffer;

        private int _position;
        private int _length;
        private int _dataCapacity;
        private int _payloadCapacity;

        public ColumnStreamWithRetry(Stream underlying, TCodec codec, int capacity = 8192)
        {
            _underlying = underlying;
            _codec = codec;

            _dataCapacity = capacity / sizeof(T) * sizeof(T);
            _payloadCapacity = _codec.GetMaxEncodedSize(_dataCapacity / sizeof(T)) + sizeof(PayloadHeader) + 1;

            _dataBuffer = new UnsafeBuffer(_dataCapacity);
            _payloadBuffer = new UnsafeBuffer(_payloadCapacity);
        }

        #region Overrides of Stream

        public int Read(byte* p, int count)
        {
            var remaining = _length - _position;
            var totalRead = 0;

            while (count > remaining)
            {
                //Buffer.MemoryCopy(_dataBuffer, p, count, remaining);
                Unsafe.CopyBlock(p, _dataBuffer.Ptr, (uint)remaining);
                p += remaining;
                totalRead += remaining;

                if (!TryReadFromUnderlying())
                    break;

                count -= remaining;
                remaining = _length;
            }

            if (count <= remaining)
            {
                //Buffer.MemoryCopy(_dataBuffer + _position, p, count, count);
                Unsafe.CopyBlock(p, _dataBuffer.Ptr + _position, (uint)count);
                _position += count;
                totalRead += count;
            }

            return totalRead;
        }

        public int Write(byte* p, int count)
        {
            var wrote = 0;

            do
            {
                var length = Math.Min(_dataCapacity - _position, count);
                Unsafe.CopyBlock(_dataBuffer.Ptr, p, (uint)length);

                _position += length;
                p += length;
                count -= length;
                wrote += length;

                if (_dataCapacity - _position == 0)
                    WriteToUnderlying();

            } while (count > 0);

            _length = _position;
            return wrote;
        }

        public void Reset()
        {
            if (_underlying.CanSeek)
                _underlying.Seek(0, SeekOrigin.Begin); // Todo: It can't work every time

            _position = 0;
            _length = 0;
        }

        public void Flush()
        {
            if (_position > 0)
                WriteToUnderlying();
            _underlying.Flush();
        }

        private bool TryReadFromUnderlying()
        {
            // Read payload header
            if (!TryRead(_underlying, _payloadBuffer.Data, 0, sizeof(PayloadHeader)))
            {
                _position = _length = 0;
                return false;
            }

            var payload = (PayloadHeader*)_payloadBuffer.Ptr;

            // Rescale buffers if necessary
            if (payload->DataLength > _dataCapacity)
                _dataBuffer.UpdateSize(_dataCapacity = payload->DataLength);
            if (payload->PayloadLength > _payloadCapacity)
                _payloadBuffer.UpdateSize(_payloadCapacity = payload->PayloadLength);

            // Read payload
            if (!TryRead(_underlying, _payloadBuffer.Data, sizeof(PayloadHeader), payload->PayloadLength + 1))
            {
                _position = _length = 0;
                return false;
            }

            // Decode payload to the data buffer
            _codec.Decode(_payloadBuffer.Ptr + sizeof(PayloadHeader), payload->PayloadLength, (T*)_dataBuffer.Ptr);
            _length = payload->DataLength;
            _position = 0;

            // Validate the checksum
            if (*(_payloadBuffer.Ptr + sizeof(PayloadHeader) + payload->PayloadLength) != CHECKSUM)
                throw new CorruptedDataException("Invalid checksum at the end of column block");

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryRead(Stream reader, byte[] buffer, int offset, int length)
        {
            do
            {
                var read = reader.Read(buffer, offset, length);
                if (read <= 0) return false;
                offset += read;
                length -= read;
            } while (length > 0);

            return true;
        }

        private void WriteToUnderlying()
        {
            var wrote = sizeof(PayloadHeader);

            var payload = (PayloadHeader*)_payloadBuffer.Ptr;
            payload->Version = CURRENT_VERSION;
            payload->DataLength = _position;
            payload->PayloadLength = _codec.Encode((T*)_dataBuffer.Ptr, _position / sizeof(T), _payloadBuffer.Ptr + wrote);
            wrote += payload->PayloadLength;

            // Write the checksum
            *(_payloadBuffer.Ptr + wrote) = CHECKSUM;
            wrote++;

            _underlying.Write(_payloadBuffer.Data, 0, wrote);
            _length = _position = 0;
        }

        #endregion

        #region Implementation of IDisposable

        public void Dispose()
        {
        }

        #endregion
    }
}
