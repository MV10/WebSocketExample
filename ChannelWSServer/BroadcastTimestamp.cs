using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ChannelWSServer
{
    internal class BroadcastTimestamp : IHostedService, IDisposable
    {
        private Timer timer;

        public BroadcastTimestamp()
        { }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var interval = TimeSpan.FromSeconds(Program.TIMESTAMP_INTERVAL_SEC);
            timer = new Timer(QueueBroadcast, null, TimeSpan.Zero, interval);
            return Task.CompletedTask;
        }

        private void QueueBroadcast(object state)
        {
            WebSocketMiddleware.Broadcast($"Server time: {DateTimeOffset.Now.ToString("o")}");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            timer?.Dispose();
        }
    }
}
