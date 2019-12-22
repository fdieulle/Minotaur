using Minotaur.Codecs;
using Minotaur.Pocs.Streams;
using Minotaur.Streams;
using NUnit.Framework;

namespace Minotaur.Tests.Streams.Pocs
{
    [TestFixture]
    public class ColumnStreamWithRetryTests : ColumnStreamTests
    {
        protected override IColumnStream CreateColumnStream<TEntry, TCodec>(int bufferSize, TCodec codec)
            => new ColumnStreamWithRetry<TEntry, TCodec>(new System.IO.MemoryStream(), codec, bufferSize);
    }
}
