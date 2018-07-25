using System;
using System.Runtime.CompilerServices;

namespace Minotaur.Native
{
    public static class Natives
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOfEntry<T>()
        {
            if (typeof(T) == typeof(int) || typeof(T) == typeof(Int32Entry))
                return Int32Entry.SIZE;
            if (typeof(T) == typeof(float) || typeof(T) == typeof(FloatEntry))
                return FloatEntry.SIZE;
            if (typeof(T) == typeof(long) || typeof(T) == typeof(Int64Entry))
                return Int64Entry.SIZE;
            if (typeof(T) == typeof(double) || typeof(T) == typeof(DoubleEntry))
                return DoubleEntry.SIZE;
            if (typeof(T) == typeof(string) || typeof(T) == typeof(StringEntry))
                return StringEntry.SIZE;

            throw new NotSupportedException($"This data type: {typeof(T)} isn't supported");
        }
    }
}
