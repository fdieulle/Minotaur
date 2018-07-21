using NUnit.Framework;

namespace Minotaur.Tests
{
    public static class Extensions
    {
        public static void Check<T>(this T[] x, int offsetX, T[] y, int offsetY, int length)
        {
            if (x == null && y == null) return;
            
            Assert.IsNotNull(x, "x is null");
            Assert.IsNotNull(y, "y is null");

            Assert.GreaterOrEqual(x.Length, offsetX + length, "X Length");
            Assert.GreaterOrEqual(y.Length, offsetY + length, "Y Length");
            for (int i = offsetX, j = offsetY, k = 0; k < length; i++, j++, k++)
                Assert.AreEqual(x[i], y[j], "for x[" + i + "] with y[" + j + "]");
        }
    }
}
