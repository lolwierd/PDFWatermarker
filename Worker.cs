using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;


namespace PDFWatermarker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private FileSystemWatcher fsWatch;


        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
         
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            _logger.LogInformation("Watching File System at " + @"C:\Users\dev\Documents\Scanned");
            fsWatch = new FileSystemWatcher
            {
                Path = @"C:\Users\dev\Documents\Scanned",
                NotifyFilter = NotifyFilters.FileName,
                Filter = "*.pdf"
            };
            fsWatch.Created  +=  FsWatch_Created;
            fsWatch.EnableRaisingEvents = true;

            while (stoppingToken.IsCancellationRequested)
            {
                fsWatch.EnableRaisingEvents = false;
            }

        }

        private async void FsWatch_Created(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation(string.Format("{0:G} | {1} | {2}",
                DateTime.Now, e.FullPath, e.ChangeType));
            await Task.Run(() => StartWatermarkProcess(e.FullPath));
            
        }
        private void StartWatermarkProcess(string filename) 
        {
            Console.WriteLine(filename);
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                CreateNoWindow = false,
                UseShellExecute = false,
                FileName = "add_watermark.exe",
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = string.Format("\"{0}\"", filename)
            };
            
            try
            {
                using Process exeProcess = Process.Start(startInfo);
                exeProcess.WaitForExit();
            }
            catch
            {
                _logger.LogError(string.Format("Some Error Occured while converting {0}", filename));
            }
           
        }
    }
}
