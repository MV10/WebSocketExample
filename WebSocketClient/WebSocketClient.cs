using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketClientExample
{
    public static class WebSocketClient
    {
        private static ClientWebSocket Socket;
        private static BlockingCollection<string> KeystrokeQueue = new BlockingCollection<string>();
        private static CancellationTokenSource SocketLoopTokenSource;
        private static CancellationTokenSource KeystrokeLoopTokenSource;

        public static async Task Start(string wsUri)
            => await Start(new Uri(wsUri));

        public static async Task Start(Uri wsUri)
        {
            Console.WriteLine($"Connecting to server {wsUri.ToString()}");

            SocketLoopTokenSource = new CancellationTokenSource();
            KeystrokeLoopTokenSource = new CancellationTokenSource();

            try
            {
                Socket = new ClientWebSocket();
                await Socket.ConnectAsync(wsUri, CancellationToken.None);
                _ = Task.Run(() => SocketProcessingLoop().ConfigureAwait(false));
                _ = Task.Run(() => KeystrokeTransmitLoop().ConfigureAwait(false));
            }
            catch (OperationCanceledException)
            {
                // normal upon task/token cancellation, disregard
            }
        }

        public static async Task Stop()
        {
            Console.WriteLine($"\nClosing connection");
            KeystrokeLoopTokenSource.Cancel();
            if (Socket == null || Socket.State != WebSocketState.Open) return;
            // close the socket first, because ReceiveAsync leaves an invalid socket (state = aborted) when the token is cancelled
            var timeout = new CancellationTokenSource(Program.CLOSE_SOCKET_TIMEOUT_MS);
            try
            {
                // after this, the socket state which change to CloseSent
                await Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing", timeout.Token);
                // now we wait for the server response, which will close the socket
                while (Socket.State != WebSocketState.Closed && !timeout.Token.IsCancellationRequested) ;
            }
            catch (OperationCanceledException)
            {
                // normal upon task/token cancellation, disregard
            }
            // whether we closed the socket or timed out, we cancel the token causing RecieveAsync to abort the socket
            SocketLoopTokenSource.Cancel();
            // the finally block at the end of the processing loop will dispose and null the Socket object
        }

        public static WebSocketState State
        {
            get => Socket?.State ?? WebSocketState.None;
        }

        public static void QueueKeystroke(string message)
            => KeystrokeQueue.Add(message);

        private static async Task SocketProcessingLoop()
        {
            var cancellationToken = SocketLoopTokenSource.Token;
            try
            {
                var buffer = WebSocket.CreateClientBuffer(4096, 4096);
                while (Socket.State != WebSocketState.Closed && !cancellationToken.IsCancellationRequested)
                {
                    var receiveResult = await Socket.ReceiveAsync(buffer, cancellationToken);
                    // if the token is cancelled while ReceiveAsync is blocking, the socket state changes to aborted and it can't be used
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        // the server is notifying us that the connection will close; send acknowledgement
                        if (Socket.State == WebSocketState.CloseReceived && receiveResult.MessageType == WebSocketMessageType.Close)
                        {
                            Console.WriteLine($"\nAcknowledging Close frame received from server");
                            KeystrokeLoopTokenSource.Cancel();
                            await Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Acknowledge Close frame", CancellationToken.None);
                        }

                        // display text or binary data
                        if (Socket.State == WebSocketState.Open && receiveResult.MessageType != WebSocketMessageType.Close)
                        {
                            string message = Encoding.UTF8.GetString(buffer.Array, 0, receiveResult.Count);
                            if (message.Length > 1) message = "\n" + message + "\n";
                            Console.Write(message);
                        }
                    }
                }
                Console.WriteLine($"Ending processing loop in state {Socket.State}");
            }
            catch (OperationCanceledException)
            {
                // normal upon task/token cancellation, disregard
            }
            catch (Exception ex)
            {
                Program.ReportException(ex);
            }
            finally
            {
                KeystrokeLoopTokenSource.Cancel();
                Socket.Dispose();
                Socket = null;
            }
        }

        private static async Task KeystrokeTransmitLoop()
        {
            var cancellationToken = KeystrokeLoopTokenSource.Token;
            while(!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(Program.KEYSTROKE_TRANSMIT_INTERVAL_MS, cancellationToken);
                    if(!cancellationToken.IsCancellationRequested && KeystrokeQueue.TryTake(out var message))
                    {
                        var msgbuf = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
                        await Socket.SendAsync(msgbuf, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
                    }
                }
                catch (OperationCanceledException)
                {
                    // normal upon task/token cancellation, disregard
                }
                catch (Exception ex)
                {
                    Program.ReportException(ex);
                }
            }
        }
    }
}
