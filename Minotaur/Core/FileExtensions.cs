using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

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
            if (string.IsNullOrEmpty(filePath)) return Disposable.Empty;

            var lockFilePath = filePath + ".lock";
            var waitedTimeMs = 0;
            FileStream lockedFile;

            while (!TryAcquireLock(lockFilePath, out lockedFile))
            {
                waitedTimeMs += Wait();
                if (IsTimedOut(waitedTimeMs, timeoutMs))
                    lockFilePath.DeleteFile();
            }

            return Disposable.Create(ReleaseLock, lockedFile);
        }

        private static bool TryAcquireLock(string filePath, out FileStream lockedFile)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    lockedFile = AcquireLock(filePath);
                    return true;
                }
            }
            catch (IOException)
            {
                // ignored
            }

            lockedFile = null;
            return false;
        }

        private static FileStream AcquireLock(string filePath)
        {
            return new FileStream(
                filePath,
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.Delete, 
                1);
        }

        private static void ReleaseLock(FileStream lockedFile)
        {
            try
            {
                File.Delete(lockedFile.Name);
                lockedFile.Dispose();
            }
            catch (IOException)
            {
                // ignored
            }
        }

        private static int Wait()
        {
            var wait = random.Next(0, 10);
            Thread.Sleep(wait);
            return wait;
        }

        private static bool IsTimedOut(int waitedMs, int timeoutMs) 
            => timeoutMs >= 0 && waitedMs > timeoutMs;

        #endregion
    }
}
