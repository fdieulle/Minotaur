using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using Minotaur.Codecs;
using Minotaur.Core;
using Minotaur.Streams;
using Minotaur.Tests;
using MemoryStream = Minotaur.Streams.MemoryStream;

namespace Minotaur.Benchmarks
{
    [AllStatisticsColumn]
    [DisassemblyDiagnoser(printSource: true, recursiveDepth: 5)]
    //[HardwareCounters(HardwareCounter.BranchMispredictions)]
    public unsafe class ColumnStreamBenchmark
    {
        private const int WROTE = 1024 * 5;
        private const int READ = 256;
        private ColumnStream<MemoryStream, VoidCodec> _csFullClassBase;
        private ColumnStream<MemoryStream, TemplateVoidCodec> _csTemplateCodecBase;
        private ColumnStream<TemplateMemoryStream, TemplateVoidCodec> _csFullTemplateBase;
        private ColumnStreamNew<MemoryStream, VoidCodec> _csFullClass;
        private ColumnStreamNew<MemoryStream, TemplateVoidCodec> _csTemplateCodec1;
        private ColumnStreamNew<TemplateMemoryStream, TemplateVoidCodec> _csFullTemplateCodec;
        private readonly IAllocator _allocator = new DummyUnmanagedAllocator();
        private readonly List<IntPtr> _unmanagedPtr = new List<IntPtr>();
        private readonly List<IStream> _streams = new List<IStream>();
        private byte* _rData;

        [GlobalSetup]
        public void Setup()
        {
            var data = Factory.CreateRandomBytes(WROTE);
            var ptr = Marshal.AllocHGlobal(READ);
            _unmanagedPtr.Add(ptr);
            _rData = (byte*)ptr;

            _csFullClassBase = CreateCsb(new VoidCodec(), data);
            _csTemplateCodecBase = CreateCsb(new TemplateVoidCodec(), data);
            _csFullTemplateBase = CreateCstb(new TemplateVoidCodec(), data);
            _csFullClass = CreateCs(new VoidCodec(), data);
            _csTemplateCodec1 = CreateCs(new TemplateVoidCodec(), data);
            _csFullTemplateCodec = CreateCst(new TemplateVoidCodec(), data);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _allocator.Dispose();
            _unmanagedPtr.ForEach(Marshal.FreeHGlobal);
            _unmanagedPtr.Clear();
            _streams.ForEach(p => p.Dispose());
            _streams.Clear();
        }

        [Benchmark(Baseline = true, Description = "Base Full class")]
        public int Baseline()
        {
            var read = 0;
            for (var i = 0; i < WROTE; i += READ)
                read += _csFullClassBase.Read(_rData, READ);
            return read;
        }

        [Benchmark(Description = "Base Template codec")]
        public int BaseTemplateCodec()
        {
            var read = 0;
            for (var i = 0; i < WROTE; i += READ)
                read += _csTemplateCodecBase.Read(_rData, READ);
            return read;
        }

        [Benchmark(Description = "Base Full template")]
        public int BaseFullTemplate()
        {
            var read = 0;
            for (var i = 0; i < WROTE; i += READ)
                read += _csFullTemplateBase.Read(_rData, READ);
            return read;
        }

        [Benchmark(Description = "Full class")]
        public int FullClass()
        {
            var read = 0;
            for (var i = 0; i < WROTE; i += READ)
                read += _csFullClass.Read(_rData, READ);
            return read;
        }

        [Benchmark(Description = "Template codec")]
        public int TemplateCodec()
        {
            var read = 0;
            for (var i = 0; i < WROTE; i += READ)
                read += _csTemplateCodec1.Read(_rData, READ);
            return read;
        }
        

        [Benchmark(Description = "Full Template")]
        public int FullTemplateCodec()
        {
            var read = 0;
            for (var i = 0; i < WROTE; i += READ)
                read += _csFullTemplateCodec.Read(_rData, READ);
            return read;
        }

        private ColumnStream<MemoryStream, TCodec> CreateCsb<TCodec>(TCodec codec, byte[] data)
            where TCodec : ICodec
        {
            var memory = new MemoryStream();

            var stream = new ColumnStream<MemoryStream, TCodec>(memory, codec, _allocator, 1024);
            stream.WriteAndReset(data, sizeof(byte));

            _streams.Add(stream);
            return stream;
        }

        private ColumnStream<TemplateMemoryStream, TCodec> CreateCstb<TCodec>(TCodec codec, byte[] data)
            where TCodec : ICodec
        {
            var memory = new TemplateMemoryStream(8192);

            var stream = new ColumnStream<TemplateMemoryStream, TCodec>(memory, codec, _allocator, 1024);
            stream.WriteAndReset(data, sizeof(byte));

            _streams.Add(stream);
            return stream;
        }
        
        private ColumnStreamNew<MemoryStream, TCodec> CreateCs<TCodec>(TCodec codec, byte[] data)
            where TCodec : ICodec
        {
            var memory = new MemoryStream();
            const int bufferSize = 1024;
            var buffer = Marshal.AllocHGlobal(bufferSize);
            _unmanagedPtr.Add(buffer);

            var stream = new ColumnStreamNew<MemoryStream, TCodec>(memory, codec, (byte*)buffer, bufferSize);
            stream.WriteAndReset(data, sizeof(byte));

            _streams.Add(stream);
            return stream;
        }

        private ColumnStreamNew<TemplateMemoryStream, TCodec> CreateCst<TCodec>(TCodec codec, byte[] data)
            where TCodec : ICodec
        {
            var memory = new TemplateMemoryStream(8192);
            const int bufferSize = 1024;
            var buffer = Marshal.AllocHGlobal(bufferSize);
            _unmanagedPtr.Add(buffer);

            var stream = new ColumnStreamNew<TemplateMemoryStream, TCodec>(memory, codec, (byte*)buffer, bufferSize);
            stream.WriteAndReset(data, sizeof(byte));

            _streams.Add(stream);
            return stream;
        }
    }

    public unsafe struct TemplateVoidCodec : ICodec
    {
        #region Implementation of ICodec

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Encode(ref byte* src, int lSrc, ref byte* dst, int lDst)
        {
            var count = Math.Min(lSrc, lDst);
            Buffer.MemoryCopy(src, dst, count, count);
            src += count;
            dst += count;
            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int DecodeHead(ref byte* src, int len)
        {
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Decode(ref byte* src, int lSrc, ref byte* dst, int lDst)
        {
            var count = Math.Min(lSrc, lDst);
            Buffer.MemoryCopy(src, dst, count, count);
            src += count;
            dst += count;
            return count;
        }

        #endregion
    }

    public unsafe struct TemplateMemoryStream : IStream
    {
        private byte* _buffer;
        private int _offset;
        private int _capacity;
        private int _length;

        public TemplateMemoryStream(int capacity)
        {
            _buffer = (byte*)Marshal.AllocHGlobal(capacity);
            _capacity = capacity;
            _offset = _length = 0;
        }

        #region Implementation of IStream

        public int Read(byte* p, int length)
        {
            length = Math.Min(_length - _offset, length);

            EnsureCapacity(_offset + length);
            Buffer.MemoryCopy(_buffer, p, length, length);
            _offset += length;

            return length;
        }

        public int Write(byte* p, int length)
        {
            EnsureCapacity(_offset + length);

            Buffer.MemoryCopy(p, _buffer, length, length);

            _offset += length;
            _length = Math.Max(_length, _offset);

            return length;
        }

        public int Seek(int seek, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _offset = seek;
                    break;
                case SeekOrigin.Current:
                    _offset += seek;
                    break;
                case SeekOrigin.End:
                    _offset = _length - seek;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin));
            }

            _length = Math.Max(_length, _offset);
            EnsureCapacity(_length);
            return seek;
        }

        public void Reset()
        {
            _offset = 0;
        }

        public void Flush() { }

        #endregion

        public void SetLength(int length)
        {
            _length = length;
        }

        #region Implementation of IDisposable

        public void Dispose()
        {
            Reset();
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCapacity(int length)
        {
            if (length <= _capacity) return;

            while (length > _capacity)
            {
                var nc = _capacity * 2;
                if (nc < _capacity) throw new OverflowException("Capacity overflow");

                _capacity *= 2;
            }

            var copy = (byte*)Marshal.AllocHGlobal(_capacity);
            Buffer.MemoryCopy(_buffer, copy, _length, _length);
            Marshal.FreeHGlobal((IntPtr)_buffer);
            _buffer = copy;
        }
    }

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
	public unsafe class ColumnStreamNew<TStream, TCodec> : IStream
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
        private byte* _blockEnd;
        private byte* _offset;
        private readonly int _capacity;

        public ColumnStreamNew(
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
                        var skip = *(int*) _offset;
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

                var remainingBytes = (int) (_blockEnd - _offset);
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
        /// It's better to write a suffisant number of column entries to optimise the compression.
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
            _underlying.Dispose();
        }
    }
}
