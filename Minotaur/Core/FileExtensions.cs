using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Minotaur.Core.Anonymous;

namespace Minotaur.Core
{
    public static class FileExtensions
    {
        #region Folder tools

        public static string CreateFolderIfNotExist(this string path)
        {
            try
            {
                if (!path.FolderExists())
                    Directory.CreateDirectory(path);
            }
            catch (Exception)
            {
                // Todo: log error here
            }

            return path;
        }

        public static bool FolderExists(this string path)
            => !string.IsNullOrEmpty(path) && Directory.Exists(path);

        public static bool DeleteFolder(this string path)
        {
            try
            {
                if (!path.FolderExists()) return true;

                Directory.Delete(path, true);
                return true;
            }
            catch (Exception)
            {
                // Todo: Log here
                return false;
            }
        }

        #endregion

        #region File tools

        public static string GetFolderPath(this string filePath) => 
            string.IsNullOrEmpty(filePath) 
                ? filePath 
                : new FileInfo(filePath).Directory?.FullName;

        public static bool FileExists(this string filePath)
            => !string.IsNullOrEmpty(filePath) && File.Exists(filePath);

        public static string MoveFileTo(this string filePath, string newFilePath)
        {
            try
            {
                if (!filePath.FileExists()) return filePath;

                GetFolderPath(newFilePath).CreateFolderIfNotExist();
                File.Move(filePath, newFilePath);
            }
            catch (Exception)
            {
                // Todo: Log here
            }

            return newFilePath;
        }

        public static IEnumerable<string> MoveToTmpFiles(this IEnumerable<string> files)
            => files.Select(MoveToTmpFile);

        public static string MoveToTmpFile(this string filePath)
            => !filePath.FileExists()
                ? filePath
                : filePath.MoveFileTo(filePath + ".tmp");

        public static bool DeleteFile(this string filePath)
        {
            try
            {
                if (!filePath.FileExists()) return true;
                File.Delete(filePath);
                return true;
            }
            catch (Exception)
            {
                // Todo: Log here
                return false;
            }
        }

        #endregion

        #region File locker

        private static readonly Random random = new Random();

        public static IDisposable LockFile(this string filePath, int timeoutMs = -1)
        {
            if (string.IsNullOrEmpty(filePath)) return AnonymousDisposable.Empty;

            var lockFilePath = filePath + ".lock";
            var waitTimeMs = 0;
            FileStream lockFile;
            while (true)
            {
                try
                {
                    if (!File.Exists(lockFilePath))
                    {
                        lockFile = new FileStream(
                            lockFilePath,
                            FileMode.CreateNew,
                            FileAccess.ReadWrite,
                            FileShare.Delete, 1);
                        break;
                    }
                }
                catch (Exception)
                {
                    // ignored
                }

                var wait = random.Next(0, 10);
                Thread.Sleep(wait);
                waitTimeMs += wait;
                if (timeoutMs >= 0 && waitTimeMs > timeoutMs)
                    lockFilePath.DeleteFile();
            }

            return new AnonymousDisposable(() =>
            {
                try
                {
                    File.Delete(lockFilePath);
                    lockFile.Dispose();
                }
                catch (Exception)
                {
                    // ignored
                }
            });
        }

        #endregion
    }
}
