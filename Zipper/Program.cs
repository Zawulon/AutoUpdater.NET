using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Zipper
{
    class Program
    {

        static void Main(string[] args)
        {
            BackgroundWorker _backgroundWorker;
            if (args.Length >= 3)
            {
                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        if (process.MainModule.FileName.Equals(args[2]))
                        {
                            Console.WriteLine(@"Waiting for application to Exit...");
                            process.WaitForExit();
                        }
                    }
                    catch (Exception exception)
                    {
                        Debug.WriteLine(exception.Message);
                    }
                }

                // Extract all the files.
                _backgroundWorker = new BackgroundWorker
                {
                    WorkerReportsProgress = true,
                    WorkerSupportsCancellation = true
                };

                _backgroundWorker.DoWork += (o, eventArgs) =>
                {
                    var path = Path.GetDirectoryName(args[2]);

                    // Open an existing zip file for reading.
                    ZipStorer zip = ZipStorer.Open(args[1], FileAccess.Read);

                    // Read the central directory collection.
                    List<ZipStorer.ZipFileEntry> dir = zip.ReadCentralDir();

                    for (var index = 0; index < dir.Count; index++)
                    {
                        if (_backgroundWorker.CancellationPending)
                        {
                            eventArgs.Cancel = true;
                            zip.Close();
                            return;
                        }
                        ZipStorer.ZipFileEntry entry = dir[index];
                        zip.ExtractFile(entry, Path.Combine(path, entry.FilenameInZip));
                        _backgroundWorker.ReportProgress((index + 1) * 100 / dir.Count, string.Format("Extracting {0}", entry.FilenameInZip));
                    }

                    zip.Close();
                };

                _backgroundWorker.ProgressChanged += (o, eventArgs) =>
                {
                    Console.WriteLine("{0} {1}", eventArgs.ProgressPercentage, eventArgs.UserState);
                };

                _backgroundWorker.RunWorkerCompleted += (o, eventArgs) =>
                {
                    if (!eventArgs.Cancelled)
                    {
                        Console.WriteLine(@"Finished");
                        try
                        {
                            ProcessStartInfo processStartInfo = new ProcessStartInfo(args[2]);
                            if (args.Length > 3)
                            {
                                processStartInfo.Arguments = args[3];
                            }
                            Process.Start(processStartInfo);
                        }
                        catch (Win32Exception exception)
                        {
                            if (exception.NativeErrorCode != 1223)
                                throw;
                        }
                        Environment.Exit(0);
                    }
                };
                _backgroundWorker.RunWorkerAsync();
            }
        }
    }
}
