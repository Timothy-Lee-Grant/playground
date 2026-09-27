using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// ASP.NET Core's own default KeepAliveInterval is 2 minutes — longer than YARP's
// 100s ActivityTimeout. Override with WS_KEEPALIVE_SECONDS to demonstrate the fix.
var keepAliveSecondsEnv = Environment.GetEnvironmentVariable("WS_KEEPALIVE_SECONDS");
var keepAliveInterval = keepAliveSecondsEnv is not null
    ? TimeSpan.FromSeconds(double.Parse(keepAliveSecondsEnv))
    : TimeSpan.FromMinutes(2);

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = keepAliveInterval
});

app.Logger.LogInformation("EchoServer starting. WebSocket KeepAliveInterval = {Interval}", keepAliveInterval);

app.Map("/ws", async (HttpContext context) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    app.Logger.LogInformation("Client connected. Echoing messages until the socket closes.");

    var buffer = new byte[1024];
    while (socket.State == WebSocketState.Open)
    {
        var result = await socket.ReceiveAsync(buffer, CancellationToken.None);

        if (result.MessageType == WebSocketMessageType.Close)
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None);
            break;
        }

        var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
        app.Logger.LogInformation("Echoing: {Text}", text);
        await socket.SendAsync(buffer.AsMemory(0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
    }

    app.Logger.LogInformation("Client connection loop ended. Final state: {State}", socket.State);
});

app.Run("http://localhost:5050");
