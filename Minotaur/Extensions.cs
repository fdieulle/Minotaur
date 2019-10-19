using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
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
    }
}
