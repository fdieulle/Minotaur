using System;
using System.Runtime.CompilerServices;

namespace Minotaur
{
    public static class Time
    {
        public static readonly long MaxTicks = DateTime.MaxValue.Ticks;
        public static readonly long MinTicks = DateTime.MinValue.Ticks;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime Min(DateTime x, DateTime y) => x < y ? x : y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime Max(DateTime x, DateTime y) => x > y ? x : y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpan Min(TimeSpan x, TimeSpan y) => x < y ? x : y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpan Max(TimeSpan x, TimeSpan y) => x > y ? x : y;
    }
}
