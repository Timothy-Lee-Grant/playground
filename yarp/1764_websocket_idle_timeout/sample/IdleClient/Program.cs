using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;

var uri = new Uri(args.Length > 0 ? args[0] : "ws://localhost:5000/ws");
Console.WriteLine($"Connecting to {uri} ...");

// ClientWebSocket sends its own keep-alive (an unsolicited Pong frame) every
// WebSocket.DefaultKeepAliveInterval (30 s) unless told otherwise. That traffic resets YARP's
// ActivityTimeout too. WS_CLIENT_KEEPALIVE_SECONDS overrides it; 0 disables it.
using var client = new ClientWebSocket();
var clientKeepAliveEnv = Environment.GetEnvironmentVariable("WS_CLIENT_KEEPALIVE_SECONDS");
if (clientKeepAliveEnv is not null)
{
    client.Options.KeepAliveInterval = TimeSpan.FromSeconds(double.Parse(clientKeepAliveEnv));
}
Console.WriteLine($"Client KeepAliveInterval = {client.Options.KeepAliveInterval} " +
    $"({(clientKeepAliveEnv is null ? "ClientWebSocket default" : "WS_CLIENT_KEEPALIVE_SECONDS")})");

// Optional: stop watching after this many idle seconds (for scripted runs).
var maxIdleEnv = Environment.GetEnvironmentVariable("IDLE_MAX_SECONDS");
using var watchLimit = new CancellationTokenSource();

await client.ConnectAsync(uri, CancellationToken.None);
Console.WriteLine("Connected. Sending one message to prove the round trip works.");

var helloBytes = Encoding.UTF8.GetBytes("hello");
await client.SendAsync(helloBytes, WebSocketMessageType.Text, true, CancellationToken.None);

var buffer = new byte[1024];
var echoResult = await client.ReceiveAsync(buffer, CancellationToken.None);
Console.WriteLine($"Echo received: {Encoding.UTF8.GetString(buffer, 0, echoResult.Count)}");

Console.WriteLine("Now going idle — sending nothing. Watching for the connection to die...");
var stopwatch = Stopwatch.StartNew();
if (maxIdleEnv is not null)
{
    watchLimit.CancelAfter(TimeSpan.FromSeconds(double.Parse(maxIdleEnv)));
}

using var ticker = new Timer(
    _ => Console.WriteLine($"  ...still open at {stopwatch.Elapsed.TotalSeconds:F0}s"),
    null,
    TimeSpan.FromSeconds(10),
    TimeSpan.FromSeconds(10));

try
{
    var result = await client.ReceiveAsync(buffer, watchLimit.Token);
    stopwatch.Stop();
    Console.WriteLine(
        $"[{stopwatch.Elapsed.TotalSeconds:F1}s] Received a frame instead of silence: " +
        $"{result.MessageType}, close status: {result.CloseStatus}, description: {result.CloseStatusDescription}");
}
catch (OperationCanceledException) when (watchLimit.IsCancellationRequested)
{
    stopwatch.Stop();
    Console.WriteLine($"[{stopwatch.Elapsed.TotalSeconds:F1}s] Still open after IDLE_MAX_SECONDS={maxIdleEnv}; stopping the watch.");
}
catch (WebSocketException ex)
{
    stopwatch.Stop();
    Console.WriteLine($"[{stopwatch.Elapsed.TotalSeconds:F1}s] Connection died: {ex.GetType().Name}: {ex.Message}");
}

Console.WriteLine($"Final client state: {client.State}");
