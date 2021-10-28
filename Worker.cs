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
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;

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
            _logger.LogDebug("Worker running at: {time}", DateTimeOffset.Now);
            _logger.LogDebug("Watching File System at " + PATH.PATH_TO_CHECK);
            fsWatch = new FileSystemWatcher
            {
                Path = PATH.PATH_TO_CHECK,
                NotifyFilter = NotifyFilters.FileName,
                Filter = "*.pdf"
            };
            fsWatch.Created += FsWatch_Created;
            fsWatch.EnableRaisingEvents = true;

            while (stoppingToken.IsCancellationRequested)
            {
                fsWatch.EnableRaisingEvents = false;
            }

        }

        private async void FsWatch_Created(object sender, FileSystemEventArgs e)
        {
            _logger.LogDebug(string.Format("{0:G} | {1} | {2}",
                DateTime.Now, e.FullPath, e.ChangeType));
            await Task.Run(() => WaterMarkPDF(e.FullPath));

        }

        public async void WaterMarkPDF(string filename)
        {
            try
            {
                string watermark = string.Format("-- SCANNED at {0:G} --", DateTime.Now);
                int emSize = 10;
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                File.SetAttributes(filename, File.GetAttributes(filename) & ~FileAttributes.ReadOnly);

                PdfDocument document = PdfReader.Open(filename);

                // change the version cause sometimes newer versions break it
                if (document.Version < 14)
                    document.Version = 14;

                _logger.LogDebug(string.Format("Starting Watermarking process for {0}", filename));


                XFont font = new XFont("Calibri", emSize, XFontStyle.Bold);
                for (int idx = 0; idx < document.Pages.Count; idx++)
                {
                    _logger.LogDebug(string.Format("Working for page: {0}", idx));

                    var page = document.Pages[idx];
                    var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
                    var size = gfx.MeasureString(watermark, font);

                    var format = new XStringFormat();
                    format.Alignment = XStringAlignment.Near;
                    format.LineAlignment = XLineAlignment.Near;

                    XBrush brush = new XSolidBrush(XColor.FromArgb(255, 0, 0, 0));

                    gfx.DrawString(watermark, font, brush,
                        new XPoint((size.Width)/4, (size.Height)/2),
                        format);

                }
                _logger.LogDebug("Watermarking done. Now saving file");
                document.Save(filename);
                _logger.LogDebug(string.Format("File saved at {0}", filename));

            }
            catch (Exception e)
            {
                _logger.LogError(string.Format("Some Error Occured while converting {0}", filename));
                _logger.LogError(e.Message);
                _logger.LogError(e.StackTrace);
                _logger.LogDebug("Retrying in 3 seconds!!");
                await Task.Delay(3000);
                WaterMarkPDF(filename);
            }

        }

    }
}
