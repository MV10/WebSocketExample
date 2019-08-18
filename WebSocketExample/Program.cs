using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace WebSocketExample
{
    class Program
    {
        public const int CLOSE_SOCKET_TIMEOUT_MS = 2500;

        // async Main requires C# 7.2 or newer in csproj properties
        static async Task Main(string[] args)
        {
            try
            {
                WebSocketServer.Start("http://localhost:8080/");
                Console.WriteLine("Press any key to exit...\n");
                Console.ReadKey(true);
                await WebSocketServer.StopAsync();
            }
            catch(OperationCanceledException)
            {
                // this is normal when tasks are canceled, ignore it
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