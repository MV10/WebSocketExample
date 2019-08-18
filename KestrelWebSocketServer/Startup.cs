using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace KestrelWebSocketServer
{
    public class Startup
    {

        public void ConfigureServices(IServiceCollection services)
        {
            // register our custom middleware since we use the IMiddleware factory approach
            services.AddTransient<WebSocketMiddleware>();

            // register the background process to periodically send a timestamp to clients
            services.AddHostedService<BroadcastTimestamp>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // enable websocket support
            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                ReceiveBufferSize = 4 * 1024
            });

            // add our custom middleware to the pipeline
            app.UseMiddleware<WebSocketMiddleware>();
        }
    }
}
