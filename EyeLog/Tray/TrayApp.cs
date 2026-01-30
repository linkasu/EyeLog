using System;
using System.Diagnostics;
using System.Windows.Forms;
using EyeLog.Server;
using EyeLog.Services;

namespace EyeLog.Tray
{
    internal class TrayApp : IDisposable
    {
        private readonly HttpServer server;
        private readonly ServerOptions options;
        private readonly LogBuffer logBuffer;
        private NotifyIcon icon;
        private MenuItem startItem;
        private MenuItem stopItem;

        public TrayApp(HttpServer server, ServerOptions options, LogBuffer logBuffer)
        {
            this.server = server;
            this.options = options;
            this.logBuffer = logBuffer;
        }

        public void Start()
        {
            icon = new NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Visible = true,
                Text = "EyeLog"
            };

            startItem = new MenuItem("Start server", (_, __) => StartServer());
            stopItem = new MenuItem("Stop server", (_, __) => StopServer());
            var logsItem = new MenuItem("Open logs", (_, __) => OpenLogs());
            var exitItem = new MenuItem("Exit", (_, __) => Exit());

            icon.ContextMenu = new ContextMenu(new[] { startItem, stopItem, logsItem, exitItem });
            icon.DoubleClick += (_, __) => OpenLogs();

            StartServer();
        }

        private void StartServer()
        {
            try
            {
                server.Start();
                logBuffer.Add("tray: server started");
            }
            catch (Exception ex)
            {
                logBuffer.Add("tray start error: " + ex.Message);
            }
            UpdateMenu();
        }

        private void StopServer()
        {
            try
            {
                server.Stop();
                logBuffer.Add("tray: server stopped");
            }
            catch (Exception ex)
            {
                logBuffer.Add("tray stop error: " + ex.Message);
            }
            UpdateMenu();
        }

        private void OpenLogs()
        {
            try
            {
                var url = options.Prefix + "logs";
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                logBuffer.Add("open logs error: " + ex.Message);
            }
        }

        private void Exit()
        {
            try
            {
                server.Stop();
            }
            catch
            {
            }
            Application.Exit();
        }

        private void UpdateMenu()
        {
            var running = server.IsRunning;
            if (startItem != null) startItem.Enabled = !running;
            if (stopItem != null) stopItem.Enabled = running;
        }

        public void Dispose()
        {
            if (icon != null)
            {
                icon.Visible = false;
                icon.Dispose();
            }
        }
    }
}
