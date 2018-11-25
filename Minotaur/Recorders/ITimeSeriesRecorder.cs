using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Minotaur.Core.Platform;
//using InlineIL;
using Minotaur.Native;
using Minotaur.Streams;

//using static InlineIL.IL.Emit;

namespace Minotaur.Recorders
{
    public interface ITimeSeriesRecorder : IDisposable
    {
        IEnumerable<Field> Fields { get; }

        IFieldsRecorder AddTimestamp(long ticks);
    }

    public interface IFieldsRecorder
    {
        void Record<T>(int fieldId, T value);
    }


    public unsafe class TimeSeriesRecorder<TPlatform> : ITimeSeriesRecorder, IFieldsRecorder
        where TPlatform : IPlatform
    {
        private static IStreamFactory<TPlatform> _streamFactory;

        private long _currentTicks;
        private ColumnRecorder[] _columns;
        private int _buketSize;
        private int _count;

        public TimeSeriesRecorder(IStreamFactory<TPlatform> streamFactory)
        {
            _streamFactory = streamFactory;
            _buketSize = 50;
            _columns = new ColumnRecorder[_buketSize];
            _count = 0;
        }

        #region Implementation of ITimeSeriesRecorder

        public IEnumerable<Field> Fields { get; }

        public IFieldsRecorder AddTimestamp(long ticks)
        {
            _currentTicks = ticks;
            return this;
        }

        #endregion

        #region Implementation of IFieldsRecorder

        public void Record<T>(int fieldId, T value)
        {
            GetColumn<T>(fieldId).Record<T>(_currentTicks, ref value);
        }

        #endregion

        #region Implementation of IDisposable

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        #endregion

        private ColumnRecorder GetColumn<T>(int fieldId)
        {
            if (fieldId == 0) throw new ArgumentException("Field Id = 0 can't be used because it's protected", nameof(fieldId));

            var idx = fieldId % _buketSize;
            while (_columns[idx].FieldId != 0 || _columns[idx].FieldId != fieldId)
                idx++;
            if (_columns[idx].FieldId == 0)
            {
                var size = Natives.SizeOfEntry<T>();
                //_columns[idx] = new ColumnRecorder(fieldId, , size, _streamFactory.Create());
            }

            return (ColumnRecorder)_columns[idx];
        }

        private class ColumnRecorder
        {
            private byte* _previousData;
            private byte* _data;
            private readonly IStream _stream;
            public int FieldId;

            public ColumnRecorder(int fieldId, byte* dataX2, int length, IStream stream = null)
            {
                _previousData = dataX2;
                _data = dataX2 + (length / 2);
                _stream = stream;
                FieldId = fieldId;
            }

            /// <summary>
            /// Optimzed method with templated code by jitter.
            /// </summary>
            /// <typeparam name="TTemplate">Template type for Jitter</typeparam>
            /// <typeparam name="T">Type of input value</typeparam>
            /// <param name="ticks">Ticks data corrsponding to the current timstamp.</param>
            /// <param name="value">Data value.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Record<T>(long ticks, ref T value)
            {
                if (typeof(T) == typeof(int))
                {
                    var prev = (Int32Entry*)_previousData;
                    var entry = (Int32Entry*)_data;

                    entry->value = As<T, int>(ref value);

                    if (ticks < prev->ticks || entry->value == prev->value) return;

                    entry->ticks = ticks;
                }
                else if (typeof(T) == typeof(float))
                {
                    var prev = (FloatEntry*)_previousData;
                    var entry = (FloatEntry*)_data;

                    entry->value = As<T, float>(ref value);

                    if (ticks < prev->ticks || entry->value == prev->value) return;

                    entry->ticks = ticks;
                }
                else if (typeof(T) == typeof(long))
                {
                    var prev = (Int64Entry*)_previousData;
                    var entry = (Int64Entry*)_data;

                    entry->value = As<T, long>(ref value);

                    if (ticks < prev->ticks || entry->value == prev->value) return;

                    entry->ticks = ticks;
                }
                else if (typeof(T) == typeof(double))
                {
                    var prev = (DoubleEntry*) _previousData;
                    var entry = (DoubleEntry*) _data;

                    entry->value = As<T, double>(ref value);

                    if (ticks < prev->ticks || entry->value == prev->value) return;

                    entry->ticks = ticks;
                }
                else if (typeof(T) == typeof(string))
                {
                    var prev = (StringEntry*)_previousData;
                    var entry = (StringEntry*)_data;

                    entry->SetValue(As<T, string>(ref value));

                    if (ticks < prev->ticks || entry->GetValue() == prev->GetValue()) return;

                    entry->ticks = ticks;
                }
                else throw new NotSupportedException($"This data type: {typeof(T)} isn't supported");

                // Commit previous value and roll current one.
                Write<T>(_previousData);

                Bits.XorSwap(ref _previousData, ref _data);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void Write<T>(byte* d)
            {
                if (typeof(T) == typeof(int))
                    _stream.Write(d, Int32Entry.SIZE);
                else if (typeof(T) == typeof(float))
                    _stream.Write(d, FloatEntry.SIZE);
                else if (typeof(T) == typeof(long))
                    _stream.Write(d, Int64Entry.SIZE);
                else if (typeof(T) == typeof(double))
                    _stream.Write(d, DoubleEntry.SIZE);
                else if (typeof(T) == typeof(string))
                    _stream.Write(d, StringEntry.SIZE_OF_TICKS_WITH_LENGTH + ((StringEntry*)d)->length);
                else throw new NotSupportedException($"This data type: {typeof(T)} isn't supported");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Flush<T>()
            {
                if (*(long*)_previousData < *(long*)_data)
                    Write<T>(_previousData);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                *(long*) _previousData = 0L;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static ref TTo As<TFrom, TTo>(ref TFrom source)
            {
                //Ldarg(nameof(source));
                //Ret();
                //throw IL.Unreachable();
                throw new NotImplementedException();
            }
        }
    }

    public interface IColumnRecorder<in T>
    {
        int FieldId { get; }

        void Record(long ticks, T value);

        void Flush();

        void Reset();
    }
}
