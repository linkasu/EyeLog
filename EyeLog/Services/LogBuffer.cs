using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using EyeLog.Server;

namespace EyeLog.Services
{
    internal class LogBuffer
    {
        private readonly object sync = new object();
        private readonly List<string> lines = new List<string>();
        private readonly HashSet<WebSocket> sockets = new HashSet<WebSocket>();
        private readonly int capacity;

        public LogBuffer(int capacity = 500)
        {
            this.capacity = capacity;
        }

        public void Add(string message)
        {
            var line = $"[{DateTime.UtcNow:O}] {message}";
            List<WebSocket> snapshot;
            lock (sync)
            {
                lines.Add(line);
                if (lines.Count > capacity)
                {
                    lines.RemoveAt(0);
                }
                snapshot = new List<WebSocket>(sockets);
            }

            if (snapshot.Count > 0)
            {
                var payload = Encoding.UTF8.GetBytes(line + "\n");
                WebSocketSender.SendBinary(snapshot, payload);
            }
        }

        public List<string> GetSnapshot()
        {
            lock (sync)
            {
                return new List<string>(lines);
            }
        }

        public void AddSocket(WebSocket socket)
        {
            lock (sync)
            {
                sockets.Add(socket);
            }
        }

        public void RemoveSocket(WebSocket socket)
        {
            lock (sync)
            {
                sockets.Remove(socket);
            }
        }

        public void CloseAll()
        {
            lock (sync)
            {
                foreach (var socket in sockets)
                {
                    try { socket.Abort(); } catch { }
                }
                sockets.Clear();
            }
        }
    }
}
