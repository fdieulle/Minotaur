using System.IO;
using Xunit;

namespace Minotaur.Tests.IO
{
    public class StreamTests
    {
        [Fact]
        public unsafe void MemoryStreamTests()
        {
            var data1 = Factory.CreateRandomBytes(1024 * 3 + 500);

            var memory = new Minotaur.IO.MemoryStream(1024);
            fixed (byte* p = data1)
                memory.Write(p, data1.Length);

            memory.Seek(0, SeekOrigin.Begin);

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
