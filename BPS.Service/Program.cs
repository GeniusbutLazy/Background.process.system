using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using NLog;

namespace BPS.Service
{
    internal static class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            LogManager.Setup().LoadConfigurationFromFile("NLog.config");

            try
            {
                Logger.Info("BPS.Service starting.");

                if (Environment.UserInteractive)
                {
                    var service = new Service1();
                    service.StartDebug();
                    Logger.Info("BPS.Service running in console mode. Press Enter to stop.");
                    Console.WriteLine("BPS.Service running in console mode. Press Enter to stop.");
                    Console.ReadLine();
                    service.StopDebug();
                    return;
                }

                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new Service1()
                };
                ServiceBase.Run(ServicesToRun);
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "BPS.Service terminated unexpectedly.");
                throw;
            }
            finally
            {
                Logger.Info("BPS.Service stopping.");
                LogManager.Shutdown();
            }
        }
    }
}
