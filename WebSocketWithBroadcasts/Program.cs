using System;

// This differs from WebSocketExample in that this "outer" program can
// send messages to all open sockets by calling the new Broadcast method
// on the WebSocketServer class.

namespace WebSocketWithBroadcasts
{
    class Program
    {
        const int BROADCAST_INTERVAL_SEC = 15;

        static void Main(string[] args)
        {
            try
            {
                WebSocketServer.Start("http://localhost:8080/");
                Console.WriteLine("Press any key to exit...\n");

                DateTimeOffset nextMessage = DateTimeOffset.Now.AddSeconds(BROADCAST_INTERVAL_SEC);
                while(!Console.KeyAvailable)
                {
                    if(DateTimeOffset.Now > nextMessage)
                    {
                        nextMessage = DateTimeOffset.Now.AddSeconds(BROADCAST_INTERVAL_SEC);
                        WebSocketServer.Broadcast($"Server time: {DateTimeOffset.Now.ToString("o")}");
                    }
                }

                WebSocketServer.Stop();
            }
            catch (OperationCanceledException)
            {
                // normal upon task/token cancellation, disregard
            }
            Console.WriteLine("Program ending. Press any key...");
            Console.ReadKey(true);
        }
    }
}
