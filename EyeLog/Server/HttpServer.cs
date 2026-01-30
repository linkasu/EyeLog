using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EyeLog.Models;
using EyeLog.Services;
using EyeLog.State;

namespace EyeLog.Server
{
    internal class HttpServer
    {
        private readonly HttpListener listener = new HttpListener();
        private readonly ServerOptions options;
        private readonly ClientRegistry registry;
        private readonly GazeState gazeState;
        private readonly LogBuffer logBuffer;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private Thread acceptThread;
        private Thread boundsThread;
        private Thread gazeThread;

        public bool IsRunning { get; private set; }

        public HttpServer(ServerOptions options, ClientRegistry registry, GazeState gazeState, LogBuffer logBuffer)
        {
            this.options = options;
            this.registry = registry;
            this.gazeState = gazeState;
            this.logBuffer = logBuffer;
            listener.Prefixes.Add(options.Prefix);
        }

        public void Start()
        {
            if (IsRunning)
            {
                return;
            }

            listener.Start();
            IsRunning = true;
            logBuffer.Add("server started");

            acceptThread = new Thread(AcceptLoop) { IsBackground = true };
            boundsThread = new Thread(RunBoundsProcessor) { IsBackground = true };
            gazeThread = new Thread(RunGazeBroadcaster) { IsBackground = true };

            acceptThread.Start();
            boundsThread.Start();
            gazeThread.Start();
        }

        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            cts.Cancel();
            IsRunning = false;

            try { listener.Stop(); } catch { }
            try { listener.Close(); } catch { }

            registry.CloseAll();
            logBuffer.CloseAll();
            logBuffer.Add("server stopped");
        }

        public void UpdateGaze(double x, double y, long deviceTimestamp)
        {
            gazeState.Update(x, y, deviceTimestamp);
        }

        private void AcceptLoop()
        {
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    var context = listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => HandleContext(context));
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logBuffer.Add("accept error: " + ex.Message);
                }
            }
        }

        private void HandleContext(HttpListenerContext context)
        {
            var request = context.Request;
            var path = request.Url.AbsolutePath.TrimEnd('/').ToLowerInvariant();

            if (request.HttpMethod == "OPTIONS")
            {
                HttpHelpers.WriteCors(context.Response);
                context.Response.StatusCode = 200;
                context.Response.Close();
                return;
            }

            if (request.IsWebSocketRequest)
            {
                HandleWebSocket(context, path);
                return;
            }

            if (path == "/health" && request.HttpMethod == "GET")
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
                HttpHelpers.WriteJson(context.Response, new HealthResponse { Version = version });
                return;
            }

            if (path == "/bounds" && request.HttpMethod == "POST")
            {
                HandleBoundsPost(context);
                return;
            }

            if (path == "/timeout" && request.HttpMethod == "POST")
            {
                HandleTimeoutPost(context);
                return;
            }

            if (path == "/logs" && request.HttpMethod == "GET")
            {
                HttpHelpers.WriteHtml(context.Response, HtmlTemplates.LogsPage);
                return;
            }

            context.Response.StatusCode = 404;
            context.Response.Close();
        }

        private async void HandleWebSocket(HttpListenerContext context, string path)
        {
            try
            {
                if (path == "/bounds")
                {
                    if (!TryResolveClient(context.Request, out var client))
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                        return;
                    }

                    var wsContext = await context.AcceptWebSocketAsync(null);
                    client.AddBoundsSocket(wsContext.WebSocket);
                    logBuffer.Add("bounds ws connected (" + client.ClientId + ")");
                    await ReceiveUntilClose(wsContext.WebSocket, () => client.RemoveBoundsSocket(wsContext.WebSocket));
                    return;
                }

                if (path == "/gaze")
                {
                    if (!TryResolveClient(context.Request, out var client))
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                        return;
                    }

                    var wsContext = await context.AcceptWebSocketAsync(null);
                    client.AddGazeSocket(wsContext.WebSocket);
                    logBuffer.Add("gaze ws connected (" + client.ClientId + ")");
                    await ReceiveUntilClose(wsContext.WebSocket, () => client.RemoveGazeSocket(wsContext.WebSocket));
                    return;
                }

                if (path == "/logs")
                {
                    var wsContext = await context.AcceptWebSocketAsync(null);
                    logBuffer.AddSocket(wsContext.WebSocket);
                    SendExistingLogs(wsContext.WebSocket);
                    await ReceiveUntilClose(wsContext.WebSocket, () => logBuffer.RemoveSocket(wsContext.WebSocket));
                    return;
                }

                context.Response.StatusCode = 404;
                context.Response.Close();
            }
            catch (Exception ex)
            {
                logBuffer.Add("ws error: " + ex.Message);
            }
        }

        private void HandleBoundsPost(HttpListenerContext context)
        {
            try
            {
                var payload = HttpHelpers.ReadJson<BoundsRequest>(context.Request);
                if (payload == null || payload.Bounds == null)
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                    return;
                }

                var clientId = string.IsNullOrWhiteSpace(payload.ClientId)
                    ? Guid.NewGuid().ToString("N")
                    : payload.ClientId;

                var client = registry.GetOrCreate(clientId);
                client.SetBounds(payload.Bounds);

                var response = new BoundsResponse
                {
                    ClientId = clientId,
                    Indices = BuildIndices(payload.Bounds.Count),
                    Count = payload.Bounds.Count
                };

                HttpHelpers.WriteJson(context.Response, response);
                logBuffer.Add("bounds set (" + clientId + ", " + response.Count + ")");
            }
            catch (Exception ex)
            {
                logBuffer.Add("bounds error: " + ex.Message);
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }

        private void HandleTimeoutPost(HttpListenerContext context)
        {
            try
            {
                var payload = HttpHelpers.ReadJson<TimeoutRequest>(context.Request);
                if (payload == null || string.IsNullOrWhiteSpace(payload.ClientId) || payload.ClickTimeoutMs <= 0)
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                    return;
                }

                if (!registry.TryGet(payload.ClientId, out var client))
                {
                    context.Response.StatusCode = 404;
                    context.Response.Close();
                    return;
                }

                client.SetClickTimeout(payload.ClickTimeoutMs);

                HttpHelpers.WriteJson(context.Response, new TimeoutResponse
                {
                    ClientId = payload.ClientId,
                    ClickTimeoutMs = payload.ClickTimeoutMs
                });

                logBuffer.Add("timeout set (" + payload.ClientId + ", " + payload.ClickTimeoutMs + "ms)");
            }
            catch (Exception ex)
            {
                logBuffer.Add("timeout error: " + ex.Message);
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }

        private void RunBoundsProcessor()
        {
            var processor = new BoundsProcessor(registry, gazeState, SendBoundsEvent, logBuffer.Add, options.ExitTimeoutMs, cts.Token);
            processor.Run();
        }

        private void RunGazeBroadcaster()
        {
            var broadcaster = new GazeBroadcaster(registry, gazeState, logBuffer.Add, options.GazeRateHz, options.GazeStaleMs, cts.Token);
            broadcaster.Run();
        }

        private void SendBoundsEvent(ClientState client, string type, int index, long count)
        {
            var sockets = client.GetBoundsSocketsSnapshot();
            if (sockets.Count == 0)
            {
                return;
            }

            var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string json;
            if (type == "click")
            {
                json = "{\"type\":\"click\",\"index\":" + index + ",\"count\":" + count + ",\"ts\":" + ts + "}";
            }
            else
            {
                json = "{\"type\":\"" + type + "\",\"index\":" + index + ",\"ts\":" + ts + "}";
            }
            WebSocketSender.SendJsonLine(sockets, json);
        }

        private static int[] BuildIndices(int count)
        {
            var indices = new int[count];
            for (var i = 0; i < count; i++)
            {
                indices[i] = i;
            }
            return indices;
        }

        private static async Task ReceiveUntilClose(WebSocket socket, Action onClose)
        {
            var buffer = new byte[1024];
            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                }
            }
            catch
            {
            }
            finally
            {
                try { await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", CancellationToken.None); } catch { }
                onClose?.Invoke();
            }
        }

        private bool TryResolveClient(HttpListenerRequest request, out ClientState client)
        {
            var clientId = request.QueryString["clientId"];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                client = null;
                return false;
            }

            return registry.TryGet(clientId, out client);
        }

        private void SendExistingLogs(WebSocket socket)
        {
            var snapshot = logBuffer.GetSnapshot();
            foreach (var line in snapshot)
            {
                var payload = Encoding.UTF8.GetBytes(line + "\n");
                WebSocketSender.SendBinary(new List<WebSocket> { socket }, payload);
            }
        }
    }
}
