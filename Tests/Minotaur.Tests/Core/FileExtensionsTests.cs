using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Minotaur.Core;
using Minotaur.Core.Concurrency;
using NUnit.Framework;

namespace Minotaur.Tests.Core
{
    [TestFixture]
    public class FileExtensionsTests
    {
        [Test]
        public void TestLockFile()
        {
            var fileName = $"{Guid.NewGuid():N}.dat";

            var mre1 = new ManualResetEvent(false);
            var mre2 = new ManualResetEvent(false);

            var locker1 = fileName.LockFile();
            
            Task.Run(() =>
            {
                Assert.IsTrue(mre1.WaitOne(1000));
                var locker2 = fileName.LockFile();
                mre2.Set();

                Assert.IsTrue(mre1.WaitOne(1000));
                locker2.Dispose();
            });

            // 2nd lock will be taken
            mre1.Set();
            Thread.Sleep(100);

            // Dispose the lock
            locker1.Dispose();

            Assert.IsTrue(mre2.WaitOne(1000));
        }

        [Test]
        public void TestLockFileStarvation()
        {
            const int iterations = 500;
            var fileName = $"{Guid.NewGuid():N}.dat";
            var mre = new ManualResetEvent(false);
            var counters = new int[5];
            var locked = counters.Select(p => false).ToArray();

            var threads = Enumerable.Range(0, counters.Length).Select(p => Task.Run(() =>
            {
                mre.WaitOne();

                for (var i = 0; i < iterations; i++)
                {
                    using (fileName.LockFile())
                    {
                        Assert.IsTrue(locked.All(l => !l));
                        locked[p] = true;
                        
                        counters[p] += 1;
                        Thread.Sleep(0);

                        locked[p] = false;
                    }
                }
            })).ToList();

            // Starts all threads
            mre.Set();
            
            threads.ForEach(p => Assert.IsTrue(p.Wait(10000)));

            foreach (var counter in counters)
                Assert.AreEqual(iterations, counter);
        }

        [Test]
        public void TestPlayWithFileAttributes()
        {
            var fileName = $"{Guid.NewGuid():N}.dat";
            File.Create(fileName).Dispose();

            var creationTime = File.GetCreationTimeUtc(fileName);
            var lastWriteTime = File.GetLastAccessTimeUtc(fileName);

            File.SetCreationTimeUtc(fileName, lastWriteTime.AddDays(2));
            Assert.AreNotEqual(File.GetCreationTimeUtc(fileName), creationTime);
            Assert.AreEqual(File.GetCreationTimeUtc(fileName), lastWriteTime.AddDays(2));

            File.SetLastWriteTimeUtc(fileName, lastWriteTime.AddTicks(1));
            Assert.AreNotEqual(lastWriteTime, File.GetLastWriteTimeUtc(fileName));
            Assert.AreEqual(File.GetLastWriteTimeUtc(fileName), lastWriteTime.AddTicks(1));
        }

        [Test]
        public void TestFileReadWriteLock()
        {
            const int iterations = 100;
            var fileName = $"{Guid.NewGuid():N}.dat";
            var mre = new ManualResetEvent(false);
            var counters = new int[6];
            var locked = counters.Select(p => FileAccess.ReadWrite).ToArray();

            var threads = Enumerable.Range(0, counters.Length).Select(p => Task.Run(() =>
            {
                var locker = new FileReadWriteLock(fileName);
                mre.WaitOne();

                if (p % 3 == 0)
                {
                    // Writer
                    for (var i = 0; i < iterations; i++)
                    {
                        using (locker.AcquireWrite())
                        {
                            // Check only 1 writer and no readers at a time
                            Assert.IsTrue(locked.All(l => l == FileAccess.ReadWrite), "Writer concurrency detected");
                            locked[p] = FileAccess.Write;

                            counters[p] += 1;
                            Thread.Sleep(5);

                            locked[p] = FileAccess.ReadWrite;
                        }
                    }
                }
                else
                {
                    // Reader
                    for (var i = 0; i < iterations; i++)
                    {
                        using (locker.AcquireRead())
                        {
                            // Check readers but no writer at a time
                            Assert.IsTrue(locked.All(l => l == FileAccess.ReadWrite || l == FileAccess.Read), "Reader concurrency detected");
                            locked[p] = FileAccess.Read;

                            counters[p] += 1;
                            Thread.Sleep(0);

                            locked[p] = FileAccess.ReadWrite;
                        }
                    }
                }
            })).ToList();

            // Starts all threads
            mre.Set();

            threads.ForEach(p => p.Wait());

            foreach (var counter in counters)
                Assert.AreEqual(iterations, counter);
        }

        [Test, Ignore("Not working yet")]
        public void TestMultiProcessReaderWriterLock()
        {
            const int nbReaders = 1;
            const int nbWriters = 1;
            const int nbReaderWriters = 5;
            const int durationMs = 1000;

            var fileName = $"{Guid.NewGuid():N}.dat";
            var waitFile = $"{Guid.NewGuid():N}.wait";
            //File.Create(fileName).Dispose();
            try
            {
                var processInfos = new List<ProcessStartInfo>();
                processInfos.AddRange(
                    Enumerable.Range(0, nbReaders).Select(p => new ProcessStartInfo(
                        "dotnet",
                        $"cli/Minotaur.Tests.Cli.dll lock -p {fileName} -a Read -d {durationMs} -w {waitFile}")
                        { UseShellExecute = false }));
                processInfos.AddRange(
                    Enumerable.Range(0, nbWriters).Select(p => new ProcessStartInfo(
                        "dotnet",
                        $"cli/Minotaur.Tests.Cli.dll lock -p {fileName} -a Write -d {durationMs} -w {waitFile}")
                        { UseShellExecute = false }));
                //processes.AddRange(
                //    Enumerable.Range(0, nbReaderWriters).Select(p => new ProcessStartInfo(
                //        "dotnet",
                //        $"cli/Minotaur.Tests.Cli.dll lock -p {fileName} -a ReadWrite -d {durationMs} -w {waitFile}")
                //        { UseShellExecute = false }));

                var processes = processInfos.Select(Process.Start).ToList();

                Thread.Sleep(100);
                // Starts all processes
                File.Create(waitFile).Dispose();
                
                // Wait until all processes end
                processes.ForEach(p => p.WaitForExit());
                Assert.IsTrue(processes.All(p => p.ExitCode == 0));
                Assert.IsTrue(fileName.FileExists());
            }
            finally
            {
                waitFile.DeleteFile();
                fileName.DeleteFile();
            }
        }

       
    }
}
