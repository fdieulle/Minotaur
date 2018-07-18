using System;

namespace Minotaur.Tests
{
    public static class Factory
    {
        private static readonly Random random = new Random(42);

        public static byte[] CreateRandomBytes(int count)
        {
            var data = new byte[count];
            random.NextBytes(data);
            return data;
        }
    }
}
