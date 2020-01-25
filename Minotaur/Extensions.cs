using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using Minotaur.Meta;
using Minotaur.Native;
using Minotaur.Recorders;
using Minotaur.Streams;

namespace Minotaur
{
    public static class Extensions
    {
        #region IStream extensions

        public static unsafe int Write<TStream>(this TStream stream, Array data, int itemSize)
            where TStream : IColumnStream
        {
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            var p = (byte*)handle.AddrOfPinnedObject();
            var wrote = stream.Write(p, data.Length * itemSize);
            handle.Free();
            return wrote;
        }

        public static int WriteAndReset<TStream>(this TStream stream, Array data, int itemSize)
            where TStream : IColumnStream
        {
            var wrote = stream.Write(data, itemSize);
            stream.Flush();
            stream.Reset();
            
            return wrote;
        }

        #endregion

        #region Helpers

        public static void Serialize(this XmlSerializer serializer, string filePath, object data)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                using (var writer = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    serializer.Serialize(writer, data);
            }
            catch (Exception)
            {
                // Todo: Log something here
            }
        }

        public static T Deserialize<T>(this XmlSerializer serializer, string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return default;

            try
            {
                using (var reader = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    return (T)serializer.Deserialize(reader);
            }
            catch (Exception)
            {
                // Todo: Log something here
            }

            return default;
        }

        #endregion

        #region Recorders

        public static IArrayRecorder MakeRecorder(this KeyValuePair<string, Array> pair)
            => MakeRecorder(pair.Value, pair.Key);

        public static IArrayRecorder MakeRecorder(this Array array, string column)
        {
            var elementType = array.GetType().GetElementType();
            return (IArrayRecorder)makeGenericRecorder.MakeGenericMethod(elementType)
                .Invoke(null, new object[] { column, array });
        }

        private static readonly MethodInfo makeGenericRecorder = MethodBase.GetCurrentMethod()
            .DeclaringType?.GetMethod("MakeGenericRecorder", BindingFlags.NonPublic | BindingFlags.Static);
        private static IArrayRecorder MakeGenericRecorder<T>(string column, T[] array)
            where T : unmanaged 
            => new ArrayRecorder<T>(column, array);

        #endregion

        public static List<BlockTimeSlice> Sample<TEntry, T>(this List<BlockInfo<TEntry>> blocks, int optimalBlockLength)
            where TEntry : unmanaged, IFieldEntry<T>
        {
            if (blocks == null) return null;

            var result = new List<BlockTimeSlice>();

            var offset = 0;
            var length = 0;
            for (var i = 0; i < blocks.Count; i++)
            {
                var block = blocks[i];

                if (length == 0)
                    result.Add(new BlockTimeSlice
                    {
                        Start = new DateTime(block.FirstValue.Ticks),
                        Offset = offset
                    });

                var blockLength = block.ShellSize + block.PayloadLength;
                offset += blockLength;
                length += blockLength;

                if (length >= optimalBlockLength)
                {
                    result[result.Count - 1].End = new DateTime(block.LastValue.Ticks);
                    length = 0;
                }
            }

            return result;
        }
    }
}
