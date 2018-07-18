using Xunit;

namespace Minotaur.Tests
{
    public static class Extensions
    {
        public static void Check<T>(this T[] x, int offsetX, T[] y, int offsetY, int length)
        {
            if (x == null && y == null) return;

            Assert.NotNull(x);
            Assert.NotNull(y);

            Assert.True(x.Length >= offsetX + length, "X Length");
            Assert.True(y.Length >= offsetY + length, "Y Length");
            for (int i = offsetX, j = offsetY, k = 0; k < length; i++, j++, k++)
                Assert.Equal(x[i], y[j]);
        }
    }
}
