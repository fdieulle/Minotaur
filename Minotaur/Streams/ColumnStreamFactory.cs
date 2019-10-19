using System;
using Minotaur.Codecs;
using Minotaur.Core;
using Minotaur.Cursors;
using Minotaur.IO;
using Minotaur.Meta;
using Minotaur.Native;

namespace Minotaur.Streams
{
    public class ColumnStreamFactory<TStream>
        where TStream : IStream
    {
        public IColumnCursor CreateCursor(ColumnInfo column, TStream stream, IAllocator allocator)
        {
            switch (column.Type)
            {
                case FieldType.Float:
                    return CreateCursor<FloatEntry, float>(stream, allocator);
                case FieldType.Double:
                    return CreateCursor<DoubleEntry, double>(stream, allocator);
                case FieldType.Int32:
                    return CreateCursor<Int32Entry, int>(stream, allocator);
                case FieldType.Int64:
                    return CreateCursor<Int64Entry, long>(stream, allocator);
                case FieldType.String:
                    return CreateCursor<StringEntry, string>(stream, allocator);
                default:
                    throw new NotSupportedException($"Type not supported: {column.Type}, for column : {column.Name}");
            }
        }

        private ColumnCursor<TEntry, T, ColumnStream<TEntry>> CreateCursor<TEntry, T>(IStream stream, IAllocator allocator)
            where TEntry : unmanaged, IFieldEntry<T> 
            => new ColumnCursor<TEntry, T, ColumnStream<TEntry>>(allocator, CreateColumnStream<TEntry, T>(stream));

        public IColumnStream CreateStream(ColumnInfo column, TStream stream)
        {
            switch (column.Type)
            {
                case FieldType.Float:
                    return CreateColumnStream<FloatEntry, float>(stream);
                case FieldType.Double:
                    return CreateColumnStream<DoubleEntry, double>(stream);
                case FieldType.Int32:
                    return CreateColumnStream<Int32Entry, int>(stream);
                case FieldType.Int64:
                    return CreateColumnStream<Int64Entry, long>(stream);
                case FieldType.String:
                    return CreateColumnStream<StringEntry, string>(stream);
                default:
                    throw new NotSupportedException($"Type not supported: {column.Type}, for column : {column.Name}");
            }
        }

        public ColumnStream<TEntry> CreateColumnStream<TEntry, T>(IStream stream) 
            where TEntry : unmanaged, IFieldEntry<T>
            => new ColumnStream<TEntry>(stream, new VoidCodec<TEntry>());
    }
}