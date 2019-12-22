using Minotaur.Codecs;
using Minotaur.Core;
using Minotaur.Pocs.Codecs;
using Minotaur.Pocs.Streams;
using Minotaur.Streams;
using NUnit.Framework;

namespace Minotaur.Tests.Streams.Pocs
{
    [TestFixture]
    public class ColumnStreamFullStreamTests : ColumnStreamTests
    {
        private IAllocator _allocator;

        #region Overrides of ColumnStreamTests

        protected override void OnSetup()
        {
            _allocator = new DummyPinnedAllocator();
        }

        protected override void OnTeardown()
        {
            _allocator.Dispose();
        }

        #endregion

        protected override IColumnStream CreateColumnStream<TEntry, TCodec>(int bufferSize, TCodec codec)
            => new ColumnStreamFullStream<ColumnMemoryStream, VoidCodecFullStream>(
                new ColumnMemoryStream(),
                new VoidCodecFullStream(),
                _allocator,
                bufferSize);
    }
}
