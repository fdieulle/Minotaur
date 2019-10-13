using System.IO;
using Minotaur.Streams;
using NUnit.Framework;

namespace Minotaur.Tests.Streams
{
    [TestFixture]
    public class StreamTests
    {
        [Test]
        public unsafe void MemoryStreamTests()
        {
            var data1 = Factory.CreateRandomBytes(1024 * 3 + 500);

            var memory = new ColumnMemoryStream(1024);
            fixed (byte* p = data1)
                memory.Write(p, data1.Length);

            memory.Reset();

            var data2 = new byte[1024];
            fixed (byte* p = data2)
                memory.Read(p, data2.Length);

            data1.Check(0, data2, 0, data2.Length);

            fixed (byte* p = data2)
                memory.Read(p, data2.Length);

            data1.Check(1024, data2, 0, data2.Length);

            fixed (byte* p = data2)
                memory.Read(p, data2.Length);

            data1.Check(1024 * 2, data2, 0, data2.Length);

            fixed (byte* p = data2)
                memory.Read(p, data2.Length);

            data1.Check(1024 * 3, data2, 0, 500);
        }
    }
}
