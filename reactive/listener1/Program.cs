using listener1;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Listener>();

var host = builder.Build();
host.Run();
