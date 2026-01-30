using System;
using System.Windows.Forms;
using EyeLog.Server;
using EyeLog.Services;
using EyeLog.State;
using EyeLog.Tray;

namespace EyeLog
{
    internal class Program
    {
        [STAThread]
        private static void Main()
        {
            using (var instance = SingleInstance.Create("EyeLog.Server.81203"))
            {
                if (!instance.IsOwner)
                {
                    return;
                }

                var options = new ServerOptions
                {
                    Port = 81203,
                    Host = "localhost",
                    UseHttps = true
                };

                var registry = new ClientRegistry();
                var gazeState = new GazeState();
                var logBuffer = new LogBuffer();
                var server = new HttpServer(options, registry, gazeState, logBuffer);

                using (var gazeService = new TobiiGazeService())
                using (var trayApp = new TrayApp(server, options, logBuffer))
                {
                    try
                    {
                        gazeService.Start(server.UpdateGaze);
                    }
                    catch (Exception ex)
                    {
                        logBuffer.Add("tobii init error: " + ex.Message);
                    }
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    trayApp.Start();
                    Application.Run();
                }
            }
        }
    }
}
