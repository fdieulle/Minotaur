using System;
using System.Runtime.InteropServices;

namespace Minotaur.Native
{
    [StructLayout(LayoutKind.Explicit, Size = 256)]
    public unsafe struct StringEntry : IEquatable<StringEntry>
    {
        [FieldOffset(0)]
        public int index;
        [FieldOffset(4)]
        public byte length;
        [FieldOffset(5)]
        public fixed char value[251];

        #region Equality members

        public bool Equals(StringEntry other)
        {
            if (index != other.index || length != other.length) return false;

            //fixed (char* pv = value)
            //{
            //    fixed (char* po = other.value)
            //    {
            //        for (var i = 0; i < length; i++)
            //            if (*(pv + i) != *(po + i)) return false;
            //    }
            //}

            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is StringEntry && Equals((StringEntry)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable NonReadonlyFieldInGetHashCode
                var hashCode = index;
                hashCode = (hashCode * 397) ^ length.GetHashCode();
                //hashCode = (hashCode * 397) ^ new String(value).GetHashCode();
                // ReSharper restore NonReadonlyFieldInGetHashCode
                return hashCode;
            }
        }

        public static bool operator ==(StringEntry left, StringEntry right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StringEntry left, StringEntry right)
        {
            return !left.Equals(right);
        }

        #endregion

        public override string ToString()
        {
            return $"Index: {index}, Value: {null}";
        }
    }
}