using System;
using System.Runtime.InteropServices;

namespace Minotaur.Native
{
    [StructLayout(LayoutKind.Explicit, Size = SIZE)]
    public struct DoubleEntry : IEquatable<DoubleEntry>
    {
        public const int SIZE = 16;

        [FieldOffset(0)]
        public long ticks;
        [FieldOffset(8)]
        public double value;

        #region IEquatable<DoubleEntry>

        public bool Equals(DoubleEntry other)
        {
            return ticks == other.ticks && value.Equals(other.value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is DoubleEntry entry && Equals(entry);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable NonReadonlyFieldInGetHashCode
                return (ticks.GetHashCode() * 397) ^ value.GetHashCode();
                // ReSharper restore NonReadonlyFieldInGetHashCode
            }
        }

        #endregion

        public override string ToString()
        {
            return $"Timestamp: {new DateTime(ticks)}, Value: {value}";
        }
    }
}
