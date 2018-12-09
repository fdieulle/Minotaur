using System;
using System.Runtime.InteropServices;

namespace Minotaur.Native
{
    [StructLayout(LayoutKind.Explicit, Size = SIZE)]
    public struct DoubleEntry : IFieldEntry<double>, IEquatable<DoubleEntry>
    {
        public const int SIZE = 16;

        [FieldOffset(0)]
        public long ticks;
        [FieldOffset(8)]
        public double value;

        #region IFieldEntry<double>

        long IFieldEntry<double>.Ticks => ticks;
        double IFieldEntry<double>.Value => value;

        public void Reset()
        {
            ticks = Time.MinTicks;
            value = double.NaN;
        }

        #endregion

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
