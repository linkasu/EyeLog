using System;
using System.Diagnostics;
using System.Windows.Forms;
using EyeLog.Server;
using EyeLog.Services;
using EyeLog.State;

namespace EyeLog.Tray
{
    internal class TrayApp : IDisposable
    {
        private readonly HttpServer server;
        private readonly ServerOptions options;
        private readonly LogBuffer logBuffer;
        private readonly ClientRegistry registry;
        private NotifyIcon icon;
        private MenuItem startItem;
        private MenuItem stopItem;
        private MenuItem setupItem;
        private ClientSetupForm setupForm;
        private string clientId = Guid.NewGuid().ToString("N");

        public TrayApp(HttpServer server, ServerOptions options, LogBuffer logBuffer, ClientRegistry registry)
        {
            this.server = server;
            this.options = options;
            this.logBuffer = logBuffer;
            this.registry = registry;
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
            setupItem = new MenuItem("Add bounds", (_, __) => OpenSetup());
            var logsItem = new MenuItem("Open logs", (_, __) => OpenLogs());
            var exitItem = new MenuItem("Exit", (_, __) => Exit());

            icon.ContextMenu = new ContextMenu(new[] { startItem, stopItem, setupItem, logsItem, exitItem });
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

        private void OpenSetup()
        {
            if (setupForm == null || setupForm.IsDisposed)
            {
                setupForm = new ClientSetupForm(options, registry, logBuffer, clientId, id => clientId = id);
            }
            setupForm.Show();
            setupForm.BringToFront();
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
            if (setupItem != null) setupItem.Enabled = true;
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
