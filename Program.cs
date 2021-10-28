using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PDFWatermarker
{
    public class Program
    {

        private static PATH_TO_CHECK_CLASS PATH = new PATH_TO_CHECK_CLASS { PATH_TO_CHECK = string.Format(@"{0}\Documents\Scanned", Environment.GetEnvironmentVariable("USERPROFILE")) };
        private static readonly RegistryKey rb = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        public static void Main(string[] args)
        {
            CheckRegistryExists();
            CreateHostBuilder(args).Build().Run();
        }

        private static void CheckRegistryExists()
        {
            using (RegistryKey readKey = rb.OpenSubKey(@"SOFTWARE\PDFWatermarker"))
            {
                if (readKey != null)
                {
                    Object o = readKey.GetValue("PATH_TO_CHECK");
                    Console.WriteLine(o.ToString());
                    PATH.PATH_TO_CHECK = o.ToString();
                }
                else
                {
                    using RegistryKey writeKey = rb.CreateSubKey(@"SOFTWARE\PDFWatermarker");

                    writeKey.SetValue("PATH_TO_CHECK", PATH.PATH_TO_CHECK);
                }
            }

           
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(loggerFactory => loggerFactory.AddEventLog())
                .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                    services.AddSingleton(PATH);
                });

        public class PATH_TO_CHECK_CLASS  {
            public string PATH_TO_CHECK { get; set; }
            public string PATH_TO_SAVE { get; set; }
        }
    }
}
