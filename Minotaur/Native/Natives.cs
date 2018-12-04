using System;
using System.Runtime.CompilerServices;

namespace Minotaur.Native
{
    public static unsafe class Natives
    {
        public const int MAX_ENTRY_SIZE = StringEntry.SIZE;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOfEntry<T>()
        {
            if (typeof(T) == typeof(int) || typeof(T) == typeof(Int32Entry))
                return sizeof(Int32Entry);
            if (typeof(T) == typeof(float) || typeof(T) == typeof(FloatEntry))
                return sizeof(FloatEntry);
            if (typeof(T) == typeof(long) || typeof(T) == typeof(Int64Entry))
                return sizeof(Int64Entry);
            if (typeof(T) == typeof(double) || typeof(T) == typeof(DoubleEntry))
                return sizeof(DoubleEntry);
            if (typeof(T) == typeof(string) || typeof(T) == typeof(StringEntry))
                throw new NotImplementedException();

            throw new NotSupportedException($"This data type: {typeof(T)} isn't supported");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFongible<T>(this FieldType type)
        {
            if (typeof(T) == typeof(int) || typeof(T) == typeof(Int32Entry))
                return type == FieldType.Int32;
            if (typeof(T) == typeof(float) || typeof(T) == typeof(FloatEntry))
                return type == FieldType.Float;
            if (typeof(T) == typeof(long) || typeof(T) == typeof(Int64Entry))
                return type == FieldType.Int64;
            if (typeof(T) == typeof(double) || typeof(T) == typeof(DoubleEntry))
                return type == FieldType.Double;
            if (typeof(T) == typeof(string) || typeof(T) == typeof(StringEntry))
                return type == FieldType.Float;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FieldType GetType<T>()
        {
            if (typeof(T) == typeof(int) || typeof(T) == typeof(Int32Entry))
                return FieldType.Int32;
            if (typeof(T) == typeof(float) || typeof(T) == typeof(FloatEntry))
                return FieldType.Float;
            if (typeof(T) == typeof(long) || typeof(T) == typeof(Int64Entry))
                return FieldType.Int64;
            if (typeof(T) == typeof(double) || typeof(T) == typeof(DoubleEntry))
                return FieldType.Double;
            if (typeof(T) == typeof(string) || typeof(T) == typeof(StringEntry))
                return FieldType.Float;
            return FieldType.Unknown;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteValue<T>(byte* buffer, ref T value) 
            where T : unmanaged
        {
            if (typeof(T) == typeof(string))
                throw new NotImplementedException();
            if (typeof(T) == typeof(StringEntry))
                throw new NotImplementedException();

            if (typeof(T) == typeof(Int32Entry))
                return WriteValue(buffer, ref As<T, Int32Entry>(value).value);
            if (typeof(T) == typeof(Int64Entry))
                return WriteValue(buffer, ref As<T, Int64Entry>(value).value);
            if (typeof(T) == typeof(FloatEntry))
                return WriteValue(buffer, ref As<T, FloatEntry>(value).value);
            if (typeof(T) == typeof(DoubleEntry))
                return WriteValue(buffer, ref As<T, DoubleEntry>(value).value);

            *(T*)buffer = value;
            return sizeof(T);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref TTo As<TFrom, TTo>(TFrom source) 
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            return ref *((TTo*) &source);
        }
    }
}
