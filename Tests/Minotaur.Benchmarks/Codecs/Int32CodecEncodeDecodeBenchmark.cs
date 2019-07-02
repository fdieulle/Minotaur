using System;
using BenchmarkDotNet.Attributes;
using Minotaur.Codecs;
using Minotaur.Core;
using Minotaur.Native;
using Minotaur.Pocs.Codecs.Int32;
using Minotaur.Tests;

namespace Minotaur.Benchmarks.Codecs
{
    [AllStatisticsColumn]
    public unsafe class Int32CodecEncodeDecodeBenchmark
    {
        private readonly MinDeltaInt32Codec _codec1 = new MinDeltaInt32Codec();
        private readonly MinDeltaInt32GenericCodec _codec2 = new MinDeltaInt32GenericCodec();
        private Int32Entry[] _data;
        private UnsafeBuffer _buffer;

        [Params(1024, 2048, 4096, 8196, 16384)]
        public int Size { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _data = Factory.CreateInt32Chunk(Size / sizeof(Int32Entry));
            var maxSize = Math.Max(
                _codec1.GetMaxEncodedSize(_data.Length), 
                _codec2.GetMaxEncodedSize(_data.Length));
            _buffer = new UnsafeBuffer(maxSize);
        }

        [Benchmark]
        public void Codec1()
        {
            fixed (Int32Entry* p = _data)
            {
                var length = _codec1.Encode(p, _data.Length, _buffer.Ptr);
                _codec1.Decode(_buffer.Ptr, length, p);
            }
        }

        [Benchmark]
        public void Codec2()
        {
            fixed (Int32Entry* p = _data)
            {
                var length = _codec2.Encode(p, _data.Length, _buffer.Ptr);
                _codec2.Decode(_buffer.Ptr, length, p);
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _buffer.Dispose();
        }
    }
}
