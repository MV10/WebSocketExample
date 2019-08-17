using System;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

// Requires .NET Core 3.0 or later due to a close-handshake bug in earlier versions
// which prevents the client socket from closing properly when the server responds
// to a client socket's Close frame.

namespace WebSocketClientExample
{
    public class Program
    {
        public const int KEYSTROKE_TRANSMIT_INTERVAL_MS = 100;
        public const int CLOSE_SOCKET_TIMEOUT_MS = 10000;

        // async Main requires C# 7.2 or newer in csproj properties
        static async Task Main(string[] args) 
        {
            bool running = true;
            while(running)
            {
                Console.Clear();
                await MainThreadUILoop();
                Console.WriteLine("\nPress R to re-connect or any other key to exit.");
                var key = Console.ReadKey(intercept: true);
                running = (key.Key == ConsoleKey.R);
            }
        }

        static async Task MainThreadUILoop()
        {
            try
            {
                await WebSocketClient.Start(@"ws://localhost:8080/");
                Console.WriteLine("Press ESC to exit. Other keystrokes are sent to the echo server.\n\n");
                bool running = true;
                while (running && WebSocketClient.State == WebSocketState.Open)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(intercept: true);
                        if (key.Key == ConsoleKey.Escape)
                        {
                            running = false;
                        }
                        else
                        {
                            WebSocketClient.QueueKeystroke(key.KeyChar.ToString());
                        }
                    }
                }
                await WebSocketClient.Stop();
            }
            catch (OperationCanceledException)
            {
                // normal upon task/token cancellation, disregard
            }
            catch (Exception ex)
            {
                ReportException(ex);
            }
        }

        public static void ReportException(Exception ex, [CallerMemberName]string location = "(Caller name not set)")
        {
            Console.WriteLine($"\n{location}:\n  Exception {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null) Console.WriteLine($"  Inner Exception {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
        }
    }
}
