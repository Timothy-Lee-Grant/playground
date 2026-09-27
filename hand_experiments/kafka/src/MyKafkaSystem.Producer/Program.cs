using MyKafkaSystem.Producer;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Producer>();

var host = builder.Build();
host.Run();
