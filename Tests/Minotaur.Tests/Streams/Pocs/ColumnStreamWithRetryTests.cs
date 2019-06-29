using Minotaur.Codecs;
using Minotaur.Pocs.Streams;
using Minotaur.Streams;
using NUnit.Framework;

namespace Minotaur.Tests.Streams.Pocs
{
    [TestFixture]
    public class ColumnStreamWithRetryTests : ColumnStreamTests
    {
        protected override IStream CreateColumnStream<TEntry>(int bufferSize)
            => new ColumnStreamWithRetry<TEntry, VoidCodec<TEntry>>(
                new System.IO.MemoryStream(),
                new VoidCodec<TEntry>(),
                bufferSize);
    }
}
