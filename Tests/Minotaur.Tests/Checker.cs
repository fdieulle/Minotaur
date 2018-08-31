using System;
using Minotaur.IO;
using Minotaur.Native;
using NUnit.Framework;

namespace Minotaur.Tests
{
    public static unsafe class Checker
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

        public static void Check<T>(this T data, T value, string message = null)
        {
            if(message == null)
                Assert.AreEqual(value, data);
            else Assert.AreEqual(value, data, message);
        }

        public static void CheckAndReset(this MemoryStream stream, int position)
        {
            stream.Position.Check(position);
            stream.Reset();
        }

        public static void CheckAll(this IntPtr p, int length, byte value, int start = 0)
        {
            var pp = (byte*) p;
            for (var i = start; i < length; i++)
                Assert.AreEqual(value, *(pp + i), $"At idx {i}");
        }

        public static void Check(this DoubleEntry[] x, DoubleEntry[] y)
        {
            Assert.AreEqual(x.Length, y.Length, "Length");
            fixed (DoubleEntry* px = x)
            fixed (DoubleEntry* py = y)
            {
                for (var i = 0; i < x.Length; i++)
                {
                    Assert.AreEqual((px + i)->ticks, (py + i)->ticks, "Ticks");
                    Assert.AreEqual((px + i)->value, (py + i)->value, "Value");
                }
            }
        }
    }
}
