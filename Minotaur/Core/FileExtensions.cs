using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Minotaur.Core.Anonymous;

namespace Minotaur.Core
{
    public static class FileExtensions
    {
        private static readonly string lockFileContent = $"{Environment.MachineName}:{Process.GetCurrentProcess().Id}";

        public static string CreateFolderIfNotExist(this string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
            catch (Exception e)
            {
                // Todo: log error here
            }

            return path;
        }

        public static bool FileSpinWait(this string filePath, int timeout = -1)
        {
            while (filePath.IsFileLocked() || timeout-- > 0)
                Thread.Sleep(1);

            return !filePath.IsFileLocked();
        }

        private static bool IsFileLocked(this string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;

            var lockFile = filePath.GetFileLock();
            return File.Exists(lockFile) && File.ReadAllText(lockFile) != lockFileContent;
        }

        private static string GetFileLock(this string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;
            return filePath + ".lock";
        }

        public static IDisposable FileLock(this string filePath)
        {
            if (!filePath.FileSpinWait()) return AnonymousDisposable.Empty;

            var lockFilePath = filePath.GetFileLock();
            try
            {
                File.WriteAllText(lockFilePath, lockFileContent);
            }
            catch (Exception e)
            {
                // Todo: log error here
            }

            return new AnonymousDisposable(() =>
            {
                try
                {
                    File.Delete(lockFilePath);
                }
                catch (Exception e)
                {
                    // Todo: log error here
                }
            });
        }

        public static bool FileExists(this string filePath)
            => !string.IsNullOrEmpty(filePath) && File.Exists(filePath);
    }
}
