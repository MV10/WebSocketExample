﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketWithBroadcasts
{
    public static class WebSocketServer
    {
        private static HttpListener Listener;
        private static CancellationTokenSource TokenSource;
        private static CancellationToken Token;

        private static int SocketCounter = 0;

        // The dictionary key corresponds to active socket IDs, and the BlockingCollection wraps
        // the default ConcurrentQueue to store broadcast messages for each active socket.
        private static ConcurrentDictionary<int, BlockingCollection<string>> BroadcastQueues = new ConcurrentDictionary<int, BlockingCollection<string>>();

        public static void Start(string uriPrefix)
        {
            TokenSource = new CancellationTokenSource();
            Token = TokenSource.Token;
            Listener = new HttpListener();
            Listener.Prefixes.Add(uriPrefix);
            Listener.Start();
            if (Listener.IsListening)
            {
                Console.WriteLine("Connect browser for a basic echo-back web page.");
                Console.WriteLine($"Server listening: {uriPrefix}");
                // listen on a separate thread so that Listener.Stop can interrupt GetContextAsync
                Task.Run(() => Listen().ConfigureAwait(false));
            }
            else
            {
                Console.WriteLine("Server failed to start.");
            }
        }

        public static void Stop()
        {
            if (Listener?.IsListening ?? false)
            {
                TokenSource.Cancel();
                Console.WriteLine("\nServer is stopping.");
                Listener.Stop();
                Listener.Close();
                TokenSource.Dispose();
            }
        }

        public static void Broadcast(string message)
        {
            Console.WriteLine($"Broadcast: {message}");
            foreach(var kvp in BroadcastQueues)
                kvp.Value.Add(message);
        }

        private static async Task Listen()
        {
            while (!Token.IsCancellationRequested)
            {
                HttpListenerContext context = await Listener.GetContextAsync();
                if (context.Request.IsWebSocketRequest)
                {
                    // HTTP is only the initial connection; upgrade to a client-specific websocket
                    HttpListenerWebSocketContext wsContext = null;
                    try
                    {
                        wsContext = await context.AcceptWebSocketAsync(subProtocol: null);
                        int socketId = Interlocked.Increment(ref SocketCounter);
                        Console.WriteLine($"Socket {socketId}: New connection.");
                        _ = Task.Run(() => ProcessWebSocket(wsContext, socketId).ConfigureAwait(false));
                    }
                    catch (Exception)
                    {
                        // server error if upgrade from HTTP to WebSocket fails
                        context.Response.StatusCode = 500;
                        context.Response.Close();
                        return;
                    }
                }
                else
                {
                    if (context.Request.AcceptTypes.Contains("text/html"))
                    {
                        ReadOnlyMemory<byte> HtmlPage = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(HTML));
                        context.Response.ContentType = "text/html; charset=utf-8";
                        context.Response.StatusCode = 200;
                        context.Response.StatusDescription = "OK";
                        context.Response.ContentLength64 = HtmlPage.Length;
                        await context.Response.OutputStream.WriteAsync(HtmlPage, Token);
                        await context.Response.OutputStream.FlushAsync(Token);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                    context.Response.Close();
                }
            }
        }

        private static async Task ProcessWebSocket(HttpListenerWebSocketContext context, int socketId)
        {
            var socket = context.WebSocket;

            BroadcastQueues.TryAdd(socketId, new BlockingCollection<string>());
            var broadcastTokenSource = new CancellationTokenSource();
            _ = Task.Run(() => WatchForBroadcasts(socketId, socket, broadcastTokenSource.Token));

            try
            {
                byte[] buffer = new byte[4096];
                while (socket.State == WebSocketState.Open && !Token.IsCancellationRequested)
                {
                    var receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), Token);
                    Console.WriteLine($"Socket {socketId}: Received {receiveResult.MessageType} frame ({receiveResult.Count} bytes).");
                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine($"Socket {socketId}: Closing websocket.");
                        broadcastTokenSource.Cancel();
                        BroadcastQueues.TryRemove(socketId, out _);
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", Token);
                    }
                    else
                    {
                        Console.WriteLine($"Socket {socketId}: Echoing data to queue.");
                        string message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                        BroadcastQueues[socketId].Add(message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // normal upon task/token cancellation, disregard
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nSocket {socketId}:\n  Exception {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"  Inner Exception {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            finally
            {
                socket?.Dispose();
                broadcastTokenSource?.Cancel();
                broadcastTokenSource?.Dispose();
                BroadcastQueues?.TryRemove(socketId, out _);
            }
        }

        private const int BROADCAST_WAKEUP_INTERVAL = 250; // milliseconds

        private static async Task WatchForBroadcasts(int socketId, WebSocket socket, CancellationToken socketToken)
        {
            while (!socketToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(BROADCAST_WAKEUP_INTERVAL, socketToken);
                    if (!socketToken.IsCancellationRequested && BroadcastQueues[socketId].TryTake(out var message))
                    {
                        Console.WriteLine($"Socket {socketId}: Sending from queue.");
                        var msgbuf = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
                        await socket.SendAsync(msgbuf, WebSocketMessageType.Text, endOfMessage: true, socketToken);
                    }
                }
                catch(OperationCanceledException)
                {
                    // normal upon task/token cancellation, disregard
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nSocket {socketId} broadcast task:\n  Exception {ex.GetType().Name}: {ex.Message}");
                    if (ex.InnerException != null) Console.WriteLine($"  Inner Exception {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                }
            }
        }

        private const string HTML =
@"<!DOCTYPE html>
  <meta charset=""utf-8""/>
  <title>WebSocket Echo/Broadcast Client</title>
  <script language=""javascript"" type=""text/javascript"">

  var wsUri = ""ws://localhost:8080/"";
        var output;
        var websocket;

        function init()
        {
            output = document.getElementById(""output"");
            configWebSocket();
        }

        function configWebSocket()
        {
            websocket = new WebSocket(wsUri);
            websocket.onopen = function(evt) { onOpen(evt) };
            websocket.onclose = function(evt) { onClose(evt) };
            websocket.onmessage = function(evt) { onMessage(evt) };
            websocket.onerror = function(evt) { onError(evt) };
        }

        function onOpen(evt)
        {
            emit(""SOCKET OPENED"");
            sendTextFrame(""Hello"");
        }

        function onClose(evt)
        {
            emit(""SOCKET CLOSED"");
        }

        function onMessage(evt)
        {
            emit('<span style=""color:blue;"">RECEIVED: ' + evt.data + '</span>');
        }

        function onError(evt)
        {
            emit('<span style=""color:red;"">ERROR: ' + evt.data + '</span>');
        }

        function sendTextFrame(message)
        {
            if (websocket.readyState == WebSocket.OPEN)
            {
                emit(""SENT: "" + message);
                websocket.send(message);
            }
            else
            {
                emit(""Socket not open, state: "" + websocket.readyState);
            }
        }

        function emit(message)
        {
            var pre = document.createElement(""p"");
            pre.style.wordWrap = ""break-word"";
            pre.innerHTML = message;
            output.appendChild(pre);
        }

        function clickSend()
        {
            var txt = document.getElementById(""newMessage"");
            if (txt.value.length > 0)
            {
                sendTextFrame(txt.value);
                txt.value = """";
                txt.focus();
            }
        }

        function clickClose()
        {
            if (websocket.readyState == WebSocket.OPEN)
            {
                websocket.close();
            }
            else
            {
                emit(""Socket not open, state: "" + websocket.readyState);
            }
            document.getElementById(""sender"").disabled = true;
            document.getElementById(""closer"").disabled = true;
            document.getElementById(""newMessage"").disabled = true;
        }

        window.addEventListener(""load"", init, false);

  </script>

  <h2>Multi-Client WebSocket Echo/Broadcast Test</h2>

  <p><input type=""input"" id=""newMessage"" onkeyup=""if(event.key==='Enter') clickSend()""/> <input type=""button"" id=""sender"" value=""Send"" onclick=""clickSend()""/> <input type=""button"" id=""closer"" value=""Disconnect"" onclick=""clickClose()""/>

  <div id= ""output""></div> 
";
    }
}
