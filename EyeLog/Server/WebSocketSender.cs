using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

namespace EyeLog.Server
{
    internal static class WebSocketSender
    {
        public static void SendJsonLine(List<WebSocket> sockets, string json)
        {
            var payload = Encoding.UTF8.GetBytes(json + "\n");
            SendBinary(sockets, payload);
        }

        public static void SendBinary(List<WebSocket> sockets, byte[] payload)
        {
            var segment = new ArraySegment<byte>(payload);
            foreach (var socket in sockets)
            {
                if (socket.State != WebSocketState.Open)
                {
                    continue;
                }

                try
                {
                    socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None).GetAwaiter().GetResult();
                }
                catch
                {
                }
            }
        }
    }
}
