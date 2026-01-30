using System;
using System.Threading;
using EyeLog.State;

namespace EyeLog.Services
{
    internal class BoundsProcessor
    {
        private readonly ClientRegistry registry;
        private readonly GazeState gazeState;
        private readonly Action<ClientState, string, int, long> sendEvent;
        private readonly Action<string> log;
        private readonly int exitTimeoutMs;
        private readonly CancellationToken token;

        public BoundsProcessor(ClientRegistry registry, GazeState gazeState, Action<ClientState, string, int, long> sendEvent, Action<string> log, int exitTimeoutMs, CancellationToken token)
        {
            this.registry = registry;
            this.gazeState = gazeState;
            this.sendEvent = sendEvent;
            this.log = log;
            this.exitTimeoutMs = exitTimeoutMs;
            this.token = token;
        }

        public void Run()
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (!gazeState.TryGetLatest(out var gaze))
                    {
                        foreach (var client in registry.AllClients())
                        {
                            client.ProcessBounds(gaze, true, sendEvent);
                        }
                        Thread.Sleep(10);
                        continue;
                    }

                    var now = DateTime.UtcNow;
                    var stale = (now - gaze.ReceivedAtUtc).TotalMilliseconds > exitTimeoutMs;
                    foreach (var client in registry.AllClients())
                    {
                        client.ProcessBounds(gaze, stale, sendEvent);
                    }
                }
                catch (Exception ex)
                {
                    log?.Invoke("bounds loop error: " + ex.Message);
                }

                Thread.Sleep(10);
            }
        }
    }
}
