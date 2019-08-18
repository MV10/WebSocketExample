using System;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

// uses ASP.NET Core 3.0

namespace KestrelWebSocketServer
{
    public class Program
    {
        public const int TIMESTAMP_INTERVAL_SEC = 15;
        public const int BROADCAST_TRANSMIT_INTERVAL_MS = 250;
        public const int CLOSE_SOCKET_TIMEOUT_MS = 2500;

        public static void Main(string[] args)
        {
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls(new string[] { @"http://localhost:8080/" });
                    webBuilder.UseStartup<Startup>();
                })
                .Build()
                .Run();

            // TODO test HTTPS / WSS

        }

        // should use a logger but hey, it's a demo and it's free
        public static void ReportException(Exception ex, [CallerMemberName]string location = "(Caller name not set)")
        {
            Console.WriteLine($"\n{location}:\n  Exception {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null) Console.WriteLine($"  Inner Exception {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
        }
    }
}
