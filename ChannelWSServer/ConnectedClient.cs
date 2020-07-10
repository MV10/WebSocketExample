using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ChannelWSServer
{
    public class ConnectedClient
    {
        public ConnectedClient(int socketId, WebSocket socket, TaskCompletionSource<object> taskCompletion)
        {
            SocketId = socketId;
            Socket = socket;
            TaskCompletion = taskCompletion;
            BroadcastQueue = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
        }

        public int SocketId { get; private set; }

        public WebSocket Socket { get; private set; }

        public TaskCompletionSource<object> TaskCompletion { get; private set; }

        public Channel<string> BroadcastQueue { get; private set; }

        public CancellationTokenSource BroadcastLoopTokenSource { get; set; } = new CancellationTokenSource();

        public async Task BroadcastLoopAsync()
        {
            var cancellationToken = BroadcastLoopTokenSource.Token;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    while(await BroadcastQueue.Reader.WaitToReadAsync(cancellationToken))
                    {
                        string message = await BroadcastQueue.Reader.ReadAsync();
                        Console.WriteLine($"Socket {SocketId}: Sending from queue.");
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
