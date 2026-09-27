using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;

var uri = new Uri(args.Length > 0 ? args[0] : "ws://localhost:5000/ws");
Console.WriteLine($"Connecting to {uri} ...");

using var client = new ClientWebSocket();
await client.ConnectAsync(uri, CancellationToken.None);
Console.WriteLine("Connected. Sending one message to prove the round trip works.");

var helloBytes = Encoding.UTF8.GetBytes("hello");
await client.SendAsync(helloBytes, WebSocketMessageType.Text, true, CancellationToken.None);

var buffer = new byte[1024];
var echoResult = await client.ReceiveAsync(buffer, CancellationToken.None);
Console.WriteLine($"Echo received: {Encoding.UTF8.GetString(buffer, 0, echoResult.Count)}");

Console.WriteLine("Now going idle — sending nothing. Watching for the connection to die...");
var stopwatch = Stopwatch.StartNew();

using var ticker = new Timer(
    _ => Console.WriteLine($"  ...still open at {stopwatch.Elapsed.TotalSeconds:F0}s"),
    null,
    TimeSpan.FromSeconds(10),
    TimeSpan.FromSeconds(10));

try
{
    var result = await client.ReceiveAsync(buffer, CancellationToken.None);
    stopwatch.Stop();
    Console.WriteLine(
        $"[{stopwatch.Elapsed.TotalSeconds:F1}s] Received a frame instead of silence: " +
        $"{result.MessageType}, close status: {result.CloseStatus}, description: {result.CloseStatusDescription}");
}
catch (WebSocketException ex)
{
    stopwatch.Stop();
    Console.WriteLine($"[{stopwatch.Elapsed.TotalSeconds:F1}s] Connection died: {ex.GetType().Name}: {ex.Message}");
}

Console.WriteLine($"Final client state: {client.State}");
