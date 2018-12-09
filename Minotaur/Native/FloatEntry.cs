using System;
using System.Runtime.InteropServices;

namespace Minotaur.Native
{
    [StructLayout(LayoutKind.Explicit, Size = SIZE)]
    public struct FloatEntry : IFieldEntry<float>, IEquatable<FloatEntry>
    {
        public const int SIZE = 12;

        [FieldOffset(0)]
        public long ticks;
        [FieldOffset(8)]
        public float value;

        #region IFieldEntry<double>

        long IFieldEntry<float>.Ticks => ticks;
        float IFieldEntry<float>.Value => value;

        public void Reset()
        {
            ticks = Time.MinTicks;
            value = float.NaN;
        }

        #endregion

        #region Equality members

        public bool Equals(FloatEntry other)
        {
            return ticks == other.ticks && value.Equals(other.value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is FloatEntry && Equals((FloatEntry)obj);
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

        public static bool operator ==(FloatEntry left, FloatEntry right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FloatEntry left, FloatEntry right)
        {
            return !left.Equals(right);
        }

        #endregion

        public override string ToString()
        {
            return $"Timestamp: {new DateTime(ticks)}, Value: {value}";
        }
    }
}