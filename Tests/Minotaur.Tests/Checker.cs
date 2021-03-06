﻿using System;
using Minotaur.Core;
using Minotaur.Cursors;
using Minotaur.Native;
using Minotaur.Pocs.Streams;
using Minotaur.Providers;
using Minotaur.Streams;
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

        public static void IsEqualTo<T>(this T[] x, params T[] y)
        {
            if (x == null && y == null) return;

            Assert.IsNotNull(x, "x is null");
            Assert.IsNotNull(y, "y is null");

            for(var i=0; i<x.Length; i++)
                Assert.AreEqual(x[i], y[i], "at index {0}", i);
        }

        public static void CheckAndReset(this ColumnMemoryStream stream, int position)
        {
            stream.Position.Check(position);
            stream.Reset();
        }

        public static void All(this UnsafeBuffer buffer, byte value) 
            => buffer.AllInRange(0, buffer.Length - 1, value);

        public static void AllUntil(this UnsafeBuffer buffer, int offset, byte value)
            => buffer.AllInRange(0, offset, value);

        public static void AllFrom(this UnsafeBuffer buffer, int offset, byte value)
            => buffer.AllInRange(offset, buffer.Length - 1, value);

        public static void AllInRange(this UnsafeBuffer buffer, int left, int right, byte value)
        {
            for (var i = left; i <= right; i++)
                Assert.AreEqual(value, buffer.Data[i], $"At idx {i}");
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
                    Assert.AreEqual((px + i)->ticks, (py + i)->ticks, $"Ticks at {i}, count: {x.Length}");
                    Assert.AreEqual((px + i)->value, (py + i)->value, $"Value at {i}, count: {x.Length}");
                }
            }
        }

        public static void Check(this FileMetaData meta, string symbol, string column, FieldType type, string start, string end)
        {
            Assert.AreEqual(symbol, meta.Symbol, "Symbol");
            Assert.AreEqual(column, meta.Column, "Column");
            Assert.AreEqual(type, meta.Type, "Type");
            Assert.AreEqual(start.ToDateTime(), meta.Start, "Start");
            Assert.AreEqual(end.ToDateTime(), meta.End, "End");
        }

        public static void Check<T>(this IFieldProxy<T> proxy, T value, string timestamp = null)
        {
            if(!string.IsNullOrEmpty(timestamp))
                Assert.AreEqual(timestamp.ToDateTime(), proxy.Timestamp, "Timestamp");
            Assert.AreEqual(value, proxy.Value, "Value");
        }
    }
}
