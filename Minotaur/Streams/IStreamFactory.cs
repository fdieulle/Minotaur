using System;
using System.IO;
using Minotaur.Codecs;
using Minotaur.Core;
using Minotaur.Cursors;
using Minotaur.Native;
using Minotaur.Providers;

namespace Minotaur.Streams
{
    public interface IStreamFactory<TStream> where TStream : IStream
    {
        TStream CreateReader(string filePath);
        TStream CreateWriter(string filePath);
    }

    public class FileStreamFactory : IStreamFactory<MinotaurFileStream>
    {
        #region Implementation of IStreamFactory<MinotaurFileStream>

        public MinotaurFileStream CreateReader(string filePath) => filePath.FileExists() ? new MinotaurFileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read) : null;

        public MinotaurFileStream CreateWriter(string filePath) => new MinotaurFileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);

        #endregion
    }

    public class MemoryStreamFactory : IStreamFactory<MinotaurMemoryStream>
    {
        #region Implementation of IStreamFactory<MinotaurMemoryStream>

        public MinotaurMemoryStream CreateReader(string filePath) => new MinotaurMemoryStream();

        public MinotaurMemoryStream CreateWriter(string filePath) => new MinotaurMemoryStream();

        #endregion
    }

    public interface IColumnFactory
    {
        IColumnCursor CreateCursor(ColumnInfo column, IStream stream, IAllocator allocator);

        IColumnStream CreateStream(ColumnInfo column, IStream stream);
    }

    public class ColumnStreamFactory
    {
        public IColumnCursor CreateCursor(ColumnInfo column, IStream stream, IAllocator allocator)
        {
            switch (column.Type)
            {
                case FieldType.Float:
                    return CreateCursor<FloatEntry, float>(allocator, stream);
                case FieldType.Double:
                    return CreateCursor<DoubleEntry, double>(allocator, stream);
                case FieldType.Int32:
                    return CreateCursor<Int32Entry, int>(allocator, stream);
                case FieldType.Int64:
                    return CreateCursor<Int64Entry, long>(allocator, stream);
                case FieldType.String:
                    return CreateCursor<StringEntry, string>(allocator, stream);
                default:
                    throw new NotSupportedException($"Type not supported: {column.Type}, for column : {column.Name}");
            }
        }

        private ColumnCursor<TEntry, T, ColumnStream<TEntry>> CreateCursor<TEntry, T>(IAllocator allocator, IStream stream)
            where TEntry : unmanaged, IFieldEntry<T> 
            => new ColumnCursor<TEntry, T, ColumnStream<TEntry>>(allocator, CreateColumnStream<TEntry, T>(stream));

        public IColumnStream CreateStream(ColumnInfo column, IStream stream)
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

        private ColumnStream<TEntry> CreateColumnStream<TEntry, T>(IStream stream) 
            where TEntry : unmanaged, IFieldEntry<T>
            => new ColumnStream<TEntry>(stream, new VoidCodec<TEntry>());
    }
}