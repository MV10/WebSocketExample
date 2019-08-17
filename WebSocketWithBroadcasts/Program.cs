using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

// This differs from WebSocketExample in that this "outer" program can
// send messages to all open sockets by calling the new Broadcast method
// on the WebSocketServer class.

namespace WebSocketWithBroadcasts
{
    public class Program
    {
        public const int TIMESTAMP_INTERVAL_SEC = 15;
        public const int BROADCAST_TRANSMIT_INTERVAL_MS = 250;
        public const int CLOSE_SOCKET_TIMEOUT_MS = 2500;

        // async Main requires C# 7.2 or newer in csproj properties
        static async Task Main(string[] args)
        {
            try
            {
                WebSocketServer.Start("http://localhost:8080/");
                Console.WriteLine("Press any key to exit...\n");

                DateTimeOffset nextMessage = DateTimeOffset.Now.AddSeconds(TIMESTAMP_INTERVAL_SEC);
                while(!Console.KeyAvailable)
                {
                    if(DateTimeOffset.Now > nextMessage)
                    {
                        nextMessage = DateTimeOffset.Now.AddSeconds(TIMESTAMP_INTERVAL_SEC);
                        WebSocketServer.Broadcast($"Server time: {DateTimeOffset.Now.ToString("o")}");
                    }
                }

                await WebSocketServer.Stop();
            }
            catch (OperationCanceledException)
            {
                // normal upon task/token cancellation, disregard
            }
            catch(Exception ex)
            {
                ReportException(ex);
            }

            // VS2019 prompts to close the console window by default
        }

        public static void ReportException(Exception ex, [CallerMemberName]string location = "(Caller name not set)")
        {
            Console.WriteLine($"\n{location}:\n  Exception {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null) Console.WriteLine($"  Inner Exception {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
        }
    }
}
