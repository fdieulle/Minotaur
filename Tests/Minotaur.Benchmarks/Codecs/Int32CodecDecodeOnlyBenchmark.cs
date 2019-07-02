using System;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using Minotaur.Codecs;
using Minotaur.Core;
using Minotaur.Native;
using Minotaur.Pocs.Codecs.Int32;
using Minotaur.Tests;

namespace Minotaur.Benchmarks.Codecs
{
    [AllStatisticsColumn]
    public unsafe class Int32CodecDecodeOnlyBenchmark
    {
        private readonly MinDeltaInt32Codec _codec1 = new MinDeltaInt32Codec();
        private readonly MinDeltaInt32GenericCodec _codec2 = new MinDeltaInt32GenericCodec();
        private Int32Entry[] _data;
        private UnsafeBuffer _srcBuffer;
        private UnsafeBuffer _dstBuffer;
        private int _length1;
        private int _length2;

        [Params(1024, 2048, 4096, 8196, 16384)]
        public int Size { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            var size = Size / sizeof(Int32Entry);
            _data = Factory.CreateInt32Chunk(size);
            var maxSize = Math.Max(
                _codec1.GetMaxEncodedSize(_data.Length),
                _codec2.GetMaxEncodedSize(_data.Length));
            _srcBuffer = new UnsafeBuffer(maxSize);
            _dstBuffer = new UnsafeBuffer(size);

            fixed (Int32Entry* p = _data)
            {
                _length1 = _codec1.Encode(p, _data.Length, _srcBuffer.Ptr);
                _length2 = _codec2.Encode(p, _data.Length, _srcBuffer.Ptr);
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _srcBuffer.Dispose();
            _dstBuffer.Dispose();
        }

        [Benchmark]
        public int Codec1Batch() => _codec1.Decode(_srcBuffer.Ptr, _length1, (Int32Entry*)_dstBuffer.Ptr);

        [Benchmark]
        public int Codec2Batch() => _codec2.Decode(_srcBuffer.Ptr, _length2, (Int32Entry*)_dstBuffer.Ptr);
    }
}
