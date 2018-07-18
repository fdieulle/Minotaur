using System;
using System.Runtime.InteropServices;

namespace Minotaur.Native
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct Int64Entry : IEquatable<Int64Entry>
    {
        [FieldOffset(0)]
        public long ticks;
        [FieldOffset(8)]
        public long value;

        #region Equality members

        public bool Equals(Int64Entry other)
        {
            return ticks == other.ticks && value == other.value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Int64Entry && Equals((Int64Entry)obj);
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

        public static bool operator ==(Int64Entry left, Int64Entry right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Int64Entry left, Int64Entry right)
        {
            return !left.Equals(right);
        }

        #endregion

        public override string ToString()
        {
            return $"Index: {new DateTime(ticks)}, Value: {value}";
        }
    }
}