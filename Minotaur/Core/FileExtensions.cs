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
        private static readonly string lockFileContent = $"{Environment.MachineName}:{Process.GetCurrentProcess().Id}";

        public static string CreateFolderIfNotExist(this string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
            catch (Exception)
            {
                // Todo: log error here
            }

            return path;
        }

        public static string GetFolderPath(this string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return filePath;

            return new FileInfo(filePath).Directory?.FullName;
        }

        private static bool FileSpinWait(this string filePath, int timeout = -1)
        {
            while (filePath.IsFileLocked() || timeout-- > 0)
                Thread.Sleep(10);

            return !filePath.IsFileLocked();
        }

        private static bool IsFileLocked(this string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;

            var lockFile = filePath.GetLockFilePath();
            return File.Exists(lockFile) && File.ReadAllText(lockFile) != lockFileContent;
        }

        private static string GetLockFilePath(this string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;
            return filePath + ".lock";
        }

        // Todo: Lock the file when it's write as when it's read => 2 modes
        // Todo: the goal is to supports many reads but only 1 write
        public static IDisposable FileLock(this string filePath)
        {
            if (!filePath.FileSpinWait()) return AnonymousDisposable.Empty;

            var lockFilePath = filePath.GetLockFilePath();
            try
            {
                File.WriteAllText(lockFilePath, lockFileContent);
            }
            catch (Exception)
            {
                // Todo: log error here
            }

            return new AnonymousDisposable(() =>
            {
                lockFilePath.DeleteFile();
            });
        }

        public static bool FileExists(this string filePath)
            => !string.IsNullOrEmpty(filePath) && File.Exists(filePath);

        public static string MoveFileTo(this string filePath, string newFilePath)
        {
            if (!filePath.FileExists()) return filePath;

            try
            {
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
            if (!filePath.FileExists()) return true;

            try
            {
                File.Delete(filePath);
                return true;
            }
            catch (Exception)
            {
                // Todo: Log here
                return false;
            }
        }

        public static bool FolderExists(this string path)
            => !string.IsNullOrEmpty(path) && Directory.Exists(path);

        public static bool DeleteFolder(this string path)
        {
            if (!path.FolderExists()) return true;

            try
            {
                Directory.Delete(path, true);
                return true;
            }
            catch (Exception)
            {
                // Todo: Log here
                return false;
            }
        }
    }
}
