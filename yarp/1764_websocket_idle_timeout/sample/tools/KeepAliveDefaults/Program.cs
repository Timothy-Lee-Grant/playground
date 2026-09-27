// Prints the keep-alive defaults that decide whether an idle WebSocket through YARP survives.
using System.Net.WebSockets;
using Microsoft.AspNetCore.Builder;

Console.WriteLine($"Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
Console.WriteLine($"WebSocket.DefaultKeepAliveInterval             = {WebSocket.DefaultKeepAliveInterval}");
Console.WriteLine($"ClientWebSocket().Options.KeepAliveInterval    = {new ClientWebSocket().Options.KeepAliveInterval}");
Console.WriteLine($"ClientWebSocket().Options.KeepAliveTimeout     = {new ClientWebSocket().Options.KeepAliveTimeout}");
Console.WriteLine($"ASP.NET Core WebSocketOptions.KeepAliveInterval = {new WebSocketOptions().KeepAliveInterval}");
Console.WriteLine($"ASP.NET Core WebSocketOptions.KeepAliveTimeout  = {new WebSocketOptions().KeepAliveTimeout}");
Console.WriteLine($"YARP ForwarderRequestConfig.ActivityTimeout     = (null in config => 100 s built-in default; see ForwarderRequestConfig.cs)");
