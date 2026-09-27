using listener1;
using // Path to the other file so I can do dependency injection

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Listener>();

var host = builder.Build();
host.Run();
