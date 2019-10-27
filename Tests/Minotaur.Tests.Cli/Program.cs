using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Minotaur.Core;
using Minotaur.Core.Concurrency;

namespace Minotaur.Tests.Cli
{
    public class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }
    }

    [Verb("lock", HelpText = "Play with Minotaur file locker mechanism")]
    public class FileLockerOptions : Options
    {
        [Option('p', "path", HelpText = "File path")]
        public string FilePath { get; set; }

        [Option('a', "access", HelpText = "File access mode")]
        public FileAccess Access { get; set; }

        [Option('d', "duration", HelpText = "File access retry duration in milliseconds")]
        public int RetryDurationMs { get; set; }

        [Option('w', "wait", HelpText = "The process wait starting until the specified file doesn't exist")]
        public string StartWaitForFile { get; set; }
    }

    class Program
    {
        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<FileLockerOptions>(args)
                .MapResult(RunFileLocker, RunArgsErrors);
        }

        private static int RunFileLocker(FileLockerOptions options)
        {
            var locker = new FileReadWriteLock(options.FilePath);
            var tasks = new List<Task>();
            var exceptions = new ConcurrentBag<Exception>();
            
            if (options.Access.HasFlag(FileAccess.Read))
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        while (!string.IsNullOrEmpty(options.StartWaitForFile) && !options.StartWaitForFile.FileExists())
                            Thread.Sleep(0);
                        
                        var ends = DateTime.UtcNow.AddMilliseconds(options.RetryDurationMs);
                        while (DateTime.UtcNow < ends)
                        {
                            using (locker.AcquireRead())
                            {
                                if (options.Verbose)
                                    Console.WriteLine("Data accessed to read");

                                if (options.FilePath.FileExists())
                                {
                                    using (var reader = new StreamReader(new FileStream(options.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                                    {
                                        var text = reader.ReadLine();
                                        if (options.Verbose)
                                            Console.WriteLine("Data read : {0}", text);
                                    }
                                }
                            }
                            Thread.Sleep(0);
                        }
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                    }
                }));

            if (options.Access.HasFlag(FileAccess.Write))
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        while (!string.IsNullOrEmpty(options.StartWaitForFile) && !options.StartWaitForFile.FileExists())
                            Thread.Sleep(0);

                        var ends = DateTime.UtcNow.AddMilliseconds(options.RetryDurationMs);
                        while (DateTime.UtcNow < ends)
                        {
                            using (locker.AcquireWrite())
                            {
                                if (options.Verbose)
                                    Console.WriteLine("Data accessed to write");

                                using (var writer = new StreamWriter(new FileStream(options.FilePath, FileMode.OpenOrCreate,
                                    FileAccess.Write, FileShare.None)))
                                    writer.WriteLine("Hello my data");

                                if (options.Verbose)
                                    Console.WriteLine("Data wrote");
                            }
                            Thread.Sleep(0);
                        }
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                    }

                }));

            tasks.ForEach(p => p.Wait());

            if (exceptions.IsEmpty)
                return 0;
            else
            {
                var sb = new StringBuilder();
                foreach (var exception in exceptions)
                    sb.AppendLine(Format(exception));
                File.WriteAllText($"{Guid.NewGuid():N}.errors.log", sb.ToString());
                return -1;
            }
        }

        private static int RunArgsErrors(IEnumerable<Error> errors)
        {
            Console.WriteLine("Wrong arguments !");
            foreach (var error in errors)
                Console.WriteLine(error);

            return -1;
        }

        private static string Format(Exception e)
        {
            var sb = new StringBuilder();
            while (e != null)
            {
                sb.Append("[Message] ");
                sb.Append(e.Message);
                sb.AppendLine();

                sb.Append("[Source] ");
                sb.Append(e.Source);
                sb.AppendLine();
                sb.Append("[StackTrace] ");
                sb.Append(e.StackTrace);
                sb.AppendLine();

                e = e.InnerException;
                if (e != null)
                    sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
