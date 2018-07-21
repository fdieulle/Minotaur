using System;
using System.Runtime.InteropServices;

namespace Minotaur.Native
{
    [StructLayout(LayoutKind.Explicit, Size = 256)]
    public unsafe struct StringEntry : IEquatable<StringEntry>
    {
        [FieldOffset(0)]
        public long ticks;
        [FieldOffset(8)]
        public byte length;
        [FieldOffset(9)]
        public fixed byte value[247];

        #region Equality members

        public bool Equals(StringEntry other)
        {
            if (ticks != other.ticks || length != other.length) return false;

            unchecked
            {
                fixed (byte* pv = value)
                    for (var i = 0; i < length; i++)
                        if (*(pv + i) != other.value[i])
                            return false;
            }

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
                // ReSharper disable NonReadonlyMemberInGetHashCode
                var hashCode = ticks.GetHashCode();
                hashCode = (hashCode * 397) ^ length.GetHashCode();
                fixed (byte* pv = value)
                    for (var i = 0; i < length; i++)
                        hashCode = (hashCode * 397) ^ (*(pv + i)).GetHashCode();
                // ReSharper restore NonReadonlyMemberInGetHashCode
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

        public void SetValue(char* c, int len)
        {
            length = (byte)Math.Min(247, len);
            fixed (byte* pv = value)
                for (var i = 0; i < length; i++)
                    *(pv + i) = (byte)*(c + i);
        }

        public void SetValue(string s)
        {
            fixed (char* ps = s)
                SetValue(ps, s.Length);
        }

        public string GetValue()
        {
            var array = stackalloc char[length];
            fixed (byte* pv = value)
                for (var i = 0; i < length; i++)
                    array[i] = (char)*(pv + i);
            return new string(array);
        }

        public override string ToString()
        {
            fixed (byte* pv = value)
                return $"Index: {new DateTime(ticks)}, Value: {GetValue()}";
        }
    }
}