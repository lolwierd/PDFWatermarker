using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using static PDFWatermarker.Program;

namespace PDFWatermarker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private FileSystemWatcher fsWatch;
        private readonly PATH_TO_CHECK_CLASS PATH;


        public Worker(ILogger<Worker> logger, PATH_TO_CHECK_CLASS PATH)
        {
            _logger = logger;
            this.PATH = PATH;
         
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            _logger.LogInformation("Watching File System at " + PATH.PATH_TO_CHECK);
            fsWatch = new FileSystemWatcher
            {
                Path = PATH.PATH_TO_CHECK,
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
            await Task.Run(() => StartWatermarkProcessGO(e.FullPath));
            
        }
        private async void StartWatermarkProcessGO(string filename)
        {
            Console.WriteLine(filename);
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                CreateNoWindow = false,
                UseShellExecute = false,
                FileName = "pdfcpu.exe",
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = string.Format("stamp add -v -mode text -- \"*SCANNED*\" \"points:4, sc:0.2, rot:0, pos:tl, fillc:#000000\" \"{0}\"", filename)
            };

            try
            {
                using Process exeProcess = Process.Start(startInfo);
                exeProcess.WaitForExit();
                Console.WriteLine(exeProcess.ExitCode);
                if(exeProcess.ExitCode == 1)
                {
                    await Task.Delay(3000);
                    StartWatermarkProcessGO(filename);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(string.Format("Some Error Occured while converting {0}", filename));
                _logger.LogError(e.Message);
                _logger.LogError(e.StackTrace);
               //await Task.Delay(3000);
               //StartWatermarkProcessGO(filename);

            }
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
