using System;

// loosely based on this old single-client example:
// http://paulbatum.github.io/WebSocket-Samples/HttpListenerWebSocketEcho/

namespace WebSocketExample
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                WebSocketServer.Start("http://localhost:8080/");
                Console.WriteLine("Press any key to exit...\n");
                Console.ReadKey(true);
                WebSocketServer.Stop();
            }
            catch(OperationCanceledException)
            {
                // this is normal when tasks are canceled, ignore it
            }
            Console.WriteLine("Program ending. Press any key...");
            Console.ReadKey(true);
        }
    }
}