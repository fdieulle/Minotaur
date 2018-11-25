using System;
using System.Collections.Generic;

namespace Minotaur
{
    public unsafe struct Field : IEquatable<Field>
    {
        public readonly int Id;
        public FieldType Type;
        public byte* Data;

        public Field(int id, FieldType type, byte* data = default(byte*))
        {
            Id = id;
            Type = type;
            Data = data;
        }

        #region Equality members

        public bool Equals(Field other)
        {
            return Id == other.Id && Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Field field && Equals(field);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id * 397) ^ (int)Type;
            }
        }

        public static bool operator ==(Field left, Field right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Field left, Field right)
        {
            return !left.Equals(right);
        }

        #region EqualityComparer

        private sealed class Comparer : IEqualityComparer<Field>
        {
            public bool Equals(Field x, Field y)
            {
                return x.Id == y.Id && x.Type == y.Type;
            }

            public int GetHashCode(Field obj)
            {
                unchecked
                {
                    return (obj.Id * 397) ^ (int)obj.Type;
                }
            }
        }

        public static IEqualityComparer<Field> EqualityComparer { get; } = new Comparer();

        #endregion

        #endregion

        public override string ToString()
        {
            return $"Id: {Id}, Type: {Type}";
        }
    }
}
