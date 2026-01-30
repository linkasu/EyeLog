using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using EyeLog.Models;

namespace EyeLog.State
{
    internal class ClientState
    {
        private readonly object sync = new object();
        private readonly HashSet<WebSocket> boundsSockets = new HashSet<WebSocket>();
        private readonly HashSet<WebSocket> gazeSockets = new HashSet<WebSocket>();

        public string ClientId { get; }
        public List<Bound> Bounds { get; private set; } = new List<Bound>();
        public int ClickTimeoutMs { get; private set; } = 1000;
        public int LastSelected { get; private set; } = -1;
        public long LastEnterFileTime { get; private set; }
        public long LastExitStartFileTime { get; private set; } = -1;
        public long LastClicksCount { get; private set; }
        public long LastProcessedFileTime { get; private set; }

        public ClientState(string clientId)
        {
            ClientId = clientId;
        }

        public void SetBounds(List<BoundDto> bounds)
        {
            lock (sync)
            {
                var list = new List<Bound>();
                foreach (var bound in bounds)
                {
                    list.Add(new Bound(bound.X, bound.Y, bound.Width, bound.Height));
                }
                Bounds = list;
                ResetState();
            }
        }

        public void SetClickTimeout(int timeoutMs)
        {
            lock (sync)
            {
                ClickTimeoutMs = timeoutMs;
            }
        }

        public int GetClickTimeout()
        {
            lock (sync)
            {
                return ClickTimeoutMs;
            }
        }

        public List<BoundDto> GetBoundsSnapshot()
        {
            lock (sync)
            {
                var snapshot = new List<BoundDto>();
                foreach (var bound in Bounds)
                {
                    snapshot.Add(new BoundDto
                    {
                        X = bound.X,
                        Y = bound.Y,
                        Width = bound.Width,
                        Height = bound.Height
                    });
                }
                return snapshot;
            }
        }

        public void AddBoundsSocket(WebSocket socket)
        {
            lock (sync)
            {
                boundsSockets.Add(socket);
            }
        }

        public void RemoveBoundsSocket(WebSocket socket)
        {
            lock (sync)
            {
                boundsSockets.Remove(socket);
            }
        }

        public void AddGazeSocket(WebSocket socket)
        {
            lock (sync)
            {
                gazeSockets.Add(socket);
            }
        }

        public void RemoveGazeSocket(WebSocket socket)
        {
            lock (sync)
            {
                gazeSockets.Remove(socket);
            }
        }

        public List<WebSocket> GetBoundsSocketsSnapshot()
        {
            lock (sync)
            {
                return new List<WebSocket>(boundsSockets);
            }
        }

        public List<WebSocket> GetGazeSocketsSnapshot()
        {
            lock (sync)
            {
                return new List<WebSocket>(gazeSockets);
            }
        }

        public void ProcessBounds(GazeSample gaze, bool stale, Action<ClientState, string, int, long> sendEvent)
        {
            lock (sync)
            {
                if (stale)
                {
                    if (LastSelected != -1)
                    {
                        sendEvent(this, "exit", LastSelected, 0);
                        ResetSelection();
                    }
                    return;
                }

                var fileTime = DateTime.UtcNow.ToFileTimeUtc();
                if (LastProcessedFileTime == fileTime)
                {
                    return;
                }

                var nowInside = -1;
                var bounds = Bounds;
                for (var i = 0; i < bounds.Count; i++)
                {
                    if (bounds[i].isInside((int)gaze.X, (int)gaze.Y))
                    {
                        nowInside = i;
                    }
                }

                var isEnter = LastSelected == -1 && nowInside != -1 && LastSelected != nowInside;
                var isExit = LastSelected != nowInside;
                var isStay = LastSelected != -1 && LastSelected == nowInside;

                if (isEnter)
                {
                    LastEnterFileTime = fileTime;
                    LastSelected = nowInside;
                    LastClicksCount = 0;
                    sendEvent(this, "enter", nowInside, 0);
                }

                if (isExit)
                {
                    if (LastExitStartFileTime == -1)
                    {
                        LastExitStartFileTime = fileTime;
                    }
                    else if (fileTime - LastExitStartFileTime > 50)
                    {
                        sendEvent(this, "exit", LastSelected, 0);
                        ResetSelection();
                    }
                }

                if (isStay)
                {
                    LastExitStartFileTime = -1;
                    var insideTimeMs = (fileTime - LastEnterFileTime) / 10000;
                    var clicks = ClickTimeoutMs > 0 ? insideTimeMs / ClickTimeoutMs : 0;
                    if (LastClicksCount != clicks)
                    {
                        LastClicksCount = clicks;
                        if (clicks > 0)
                        {
                            sendEvent(this, "click", LastSelected, clicks);
                        }
                    }
                }

                LastProcessedFileTime = fileTime;
            }
        }

        public void CloseAll()
        {
            lock (sync)
            {
                foreach (var socket in boundsSockets)
                {
                    TryAbort(socket);
                }
                foreach (var socket in gazeSockets)
                {
                    TryAbort(socket);
                }
                boundsSockets.Clear();
                gazeSockets.Clear();
            }
        }

        private void ResetSelection()
        {
            LastSelected = -1;
            LastExitStartFileTime = -1;
            LastClicksCount = 0;
        }

        private void ResetState()
        {
            ResetSelection();
            LastProcessedFileTime = 0;
            LastEnterFileTime = 0;
        }

        private static void TryAbort(WebSocket socket)
        {
            try { socket.Abort(); } catch { }
        }
    }
}
