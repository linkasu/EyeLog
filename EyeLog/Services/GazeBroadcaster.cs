using System;
using System.Globalization;
using System.Threading;
using EyeLog.Server;
using EyeLog.State;

namespace EyeLog.Services
{
    internal class GazeBroadcaster
    {
        private readonly ClientRegistry registry;
        private readonly GazeState gazeState;
        private readonly Action<string> log;
        private readonly int rateHz;
        private readonly int staleMs;
        private readonly CancellationToken token;

        public GazeBroadcaster(ClientRegistry registry, GazeState gazeState, Action<string> log, int rateHz, int staleMs, CancellationToken token)
        {
            this.registry = registry;
            this.gazeState = gazeState;
            this.log = log;
            this.rateHz = rateHz;
            this.staleMs = staleMs;
            this.token = token;
        }

        public void Run()
        {
            var intervalMs = Math.Max(1, 1000 / rateHz);
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var hasGaze = gazeState.TryGetLatest(out var gaze);
                    var now = DateTime.UtcNow;
                    var stale = !hasGaze || (now - gaze.ReceivedAtUtc).TotalMilliseconds > staleMs;
                    var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    foreach (var client in registry.AllClients())
                    {
                        var sockets = client.GetGazeSocketsSnapshot();
                        if (sockets.Count == 0)
                        {
                            continue;
                        }

                        if (stale)
                        {
                            var json = "{\"type\":\"nogaze\",\"ts\":" + ts + "}";
                            WebSocketSender.SendJsonLine(sockets, json);
                        }
                        else
                        {
                            var x = gaze.X.ToString(CultureInfo.InvariantCulture);
                            var y = gaze.Y.ToString(CultureInfo.InvariantCulture);
                            var json = "{\"type\":\"gaze\",\"x\":" + x + ",\"y\":" + y + ",\"ts\":" + ts + "}";
                            WebSocketSender.SendJsonLine(sockets, json);
                        }
                    }
                }
                catch (Exception ex)
                {
                    log?.Invoke("gaze loop error: " + ex.Message);
                }

                Thread.Sleep(intervalMs);
            }
        }
    }
}
