| :warning: The older `WebSocketExample` and `WebSocketWithBroadcasts` projects were based on the deprecated `HttpListener` technique and will not be updated or maintained. They are still in the repository (but not in the VS solution) only because the first two articles reference them. |
| --- |

Repository for code from my blog posts:

### 2019-Jan-15: [A Simple Multi-Client WebSocket Server](https://mcguirev10.com/2019/01/15/simple-multi-client-websocket-server.html)

### 2019-Aug-17: [How to Close a WebSocket (Correctly)](https://mcguirev10.com/2019/08/17/how-to-close-websocket-correctly.html)

### 2019-Aug-18: [A Minimal Full-Feature Kestrel WebSocket Server](https://mcguirev10.com/2019/08/18/minimal-full-feature-kestrel-websocket-server.html)

### 2020-June Update:
The `KestrelWebSocketServer` and `WebSocketClient` projects have been updated to .NET Core 3.1.x. 

The Kestrel article did not clarify how I ran the project and I think this caused problems for some people. I set up Visual Studio to run the project in console mode (not under IIS Express or IIS). I also disabled browser auto-launch, which I think will open localhost to the wrong port. In the code Kestrel is configured for localhost:8080. I simply forgot that these settings aren't saved as part of the project file.

I've just now re-tested both projects and they are working normally under Win10, as well as Raspbian Linux on a Raspberry Pi 4B. :grin:
 
### 2020-July-10: [WebSocket Channel<T> Queues](https://mcguirev10.com/2020/07/10/websocket-channel-t-queues.html)
